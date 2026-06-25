using Akka.Actor;
using ny_times_most_popular.src.Actors;
using ny_times_most_popular.src.Models;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ny_times_most_popular.src.Server
{
    internal static class RequestHandler
    {
        private static readonly ActorSystem system = ActorSystem.Create("nyt-system");
        private static readonly IActorRef articleActor = system.ActorOf(Props.Create<ArticleActor>(), "articleActor");
        private static readonly IActorRef analysisActor = system.ActorOf(Props.Create<AnalysisActor>(), "analysisActor");
        private static readonly IActorRef requestActor = system.ActorOf(Props.Create(() => new RequestActor(
            new NytService(Environment.GetEnvironmentVariable("API_KEY")!), articleActor)), "requestActor");

        public static async Task HandleRequestAsync(HttpListenerContext context)
        {
            try
            {
                if (context.Request.Url?.AbsolutePath == "/analyze")
                {
                    var articles = await articleActor.Ask<List<Article>>(new Analyze());
                    var result = await analysisActor.Ask<TopicsResult>(articles);
                    await SendAsync(context, JsonSerializer.Serialize(result));
                    return;
                }
                
                int period = int.TryParse(context.Request.QueryString["period"], out int p) ? p : 7;

                requestActor.Tell(new LoadArticles(period));

                await SendAsync(context, "{\"status\":\"processing\"}");
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
