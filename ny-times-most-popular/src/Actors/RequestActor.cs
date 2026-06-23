using Akka.Actor;
using ny_times_most_popular.src.Models;
using ny_times_most_popular.src.Server;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace ny_times_most_popular.src.Actors
{
    internal class RequestActor : ReceiveActor
    {
        private readonly NytService service;
        private readonly IActorRef articleActor;

        public RequestActor(NytService service, IActorRef articleActor)
        {
            this.service = service;
            this.articleActor = articleActor;

            Receive<LoadArticles>(msg =>
            {
                Logger.Log($"RX START {msg.period}");

                service.GetArticles(msg.period)
                    .SubscribeOn(TaskPoolScheduler.Default)
                    .Subscribe(article =>
                    {
                        Logger.Log("RX -> Actor " + article.Title);
                        articleActor.Tell(new ArticlesBatch(new List<Article> { article }));
                    },
                    ex => Logger.Log("RX ERROR " + ex.Message),
                    () => Logger.Log("RX COMPLETE"));
            });
        }
    }
}
