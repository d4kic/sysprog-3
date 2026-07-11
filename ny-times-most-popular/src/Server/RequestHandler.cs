using Akka.Actor;
using Akka.Configuration;
using ny_times_most_popular.src.Actors;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ny_times_most_popular.src.Server
{
    internal static class RequestHandler
    {
        private static readonly Config config = ConfigurationFactory.ParseString("""
             manager-disp {
                 type = ForkJoinDispatcher
                 dedicated-thread-pool { 
                    thread-count = 2 
                 }
             }
             analysis-disp {
                 type = PinnedDispatcher
                 executor = thread-pool-executor
             }
             """);

        private static readonly ActorSystem system = ActorSystem.Create("nyt-system", config);

        private static readonly IActorRef manager = system.ActorOf(
            ManagerActor.Props().WithDispatcher("manager-disp"), "managerActor");

        public static IActorRef Manager => manager;

        public static async Task HandleRequestAsync(HttpListenerContext context)
        {
            Logger.Log($"Server primio zahtev {context.Request.Url?.PathAndQuery ?? "/"}");
            try
            {
                int period = int.TryParse(context.Request.QueryString["period"], out int p) ? p : 7;
                if (period != 1 && period != 7 && period != 30)
                {
                    Logger.Log($"Neispravan period = {period}");
                    await SendAsync(context, JsonSerializer.Serialize(new { error = "period mora biti 1, 7 ili 30" }), 400);
                    return;
                }
                var result = await manager.Ask<TopicsResult>(new GetTopics(period), TimeSpan.FromSeconds(5));
                await SendAsync(context, JsonSerializer.Serialize(result));
                Logger.Log($"Zahtev uspesno obradjen: period = {period}");
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                await SendAsync(context, JsonSerializer.Serialize(new { error = ex.Message }), 500);
            }
        }

        private static async Task SendAsync(HttpListenerContext context, string data, int status = 200)
        {
            byte[] buff = Encoding.UTF8.GetBytes(data);
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buff.Length;
            await context.Response.OutputStream.WriteAsync(buff);
            context.Response.Close();
        }

        public static void Stop()
        {
            system.Terminate().Wait(TimeSpan.FromSeconds(10));
        }
    }
}
