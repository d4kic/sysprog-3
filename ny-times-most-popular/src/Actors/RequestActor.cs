using Akka.Actor;
using ny_times_most_popular.src.Server;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace ny_times_most_popular.src.Actors
{
    internal class RequestActor : ReceiveActor
    {
        private readonly NytService service;
        private readonly IActorRef analysisActor;

        public RequestActor(NytService service, IActorRef analysisActor)
        {
            this.service = service;
            this.analysisActor = analysisActor;

            ReceiveAsync<LoadArticles>(HandleLoadArticles);
        }

        private async Task HandleLoadArticles(LoadArticles msg)
        {
            var sender = Sender;
            Logger.Log($"RX start");
            try
            {
                var articles = await service.GetArticles(msg.period)
                    .SubscribeOn(TaskPoolScheduler.Default)
                    .Do(a => Logger.Log($"RX obradio {a.Title}"))
                    .ToList()
                    .Select(list => list.ToList())
                    .ToTask();
                Logger.Log($"RX complete - {articles.Count} clanaka obradjeno.");
                var result = await analysisActor.Ask<TopicsResult>(
                    new ComputeTopics(msg.period, articles), TimeSpan.FromSeconds(10));
                sender.Tell(result);
            }
            catch (Exception ex)
            {
                Logger.Log($"RX ERROR {ex.Message}");
                sender.Tell(new TopicsResult(msg.period, new List<TopicInfo>(), 0));
            }
        }
    }
}
