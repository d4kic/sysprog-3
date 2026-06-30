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
            disp {
                type = ForkJoinDispatcher
                throughput = 5
                dedicated-thread-pool {
                    thread-count = 4
                }
            }
            """);

        private static readonly ActorSystem system = ActorSystem.Create("nyt-system", config);

        private static readonly IActorRef articleActor = system.ActorOf(Props.Create<ArticleActor>(), "articleActor");
        
        private static readonly IActorRef analysisActor = system.ActorOf(Props.Create<AnalysisActor>()
            .WithDispatcher("disp"), "analysisActor");
        
        private static readonly IActorRef requestActor = system.ActorOf(Props.Create(() => 
        new RequestActor(new NytService(Environment.GetEnvironmentVariable("API_KEY")!),
            articleActor, analysisActor)), "requestActor");

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
                var result = await requestActor.Ask<TopicsResult>(new LoadArticles(period), TimeSpan.FromSeconds(10));
                await SendAsync(context, JsonSerializer.Serialize(result));
                Logger.Log($"Zahtev uspesno obradjen");
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
    }
}
