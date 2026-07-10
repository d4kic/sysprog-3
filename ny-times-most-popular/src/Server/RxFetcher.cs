using Akka.Actor;
using ny_times_most_popular.src.Actors;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace ny_times_most_popular.src.Server
{
    internal class RxFetcher
    {
        private readonly NytService service;
        private readonly IActorRef analysisActor;
        private readonly int[] periods = { 1, 7, 30 };

        public RxFetcher(NytService service, IActorRef analysisActor)
        {
            this.service = service;
            this.analysisActor = analysisActor;
        }

        public void Start()
        {
            Observable.Interval(TimeSpan.FromSeconds(30))
                .StartWith(0)
                .SubscribeOn(TaskPoolScheduler.Default)
                .Subscribe(_ =>
                {
                    Logger.Log("RxFetcher: nabavlja clanke...");
                    foreach (var period in periods)
                        FetchPeriod(period);
                });
        }

        private void FetchPeriod(int period)
        {
            Logger.Log($"RX: Fetch za period = {period}");
            analysisActor.Tell(new ClearArticles(period));

            service.GetArticles(period)
                .SubscribeOn(TaskPoolScheduler.Default)
                .Subscribe(
                    onNext: a =>
                    {
                        Logger.Log($"RX {period} obradio: {a.Title}");
                        analysisActor.Tell(new StoreArticle(period, a));
                    },
                    onError: ex =>
                    {
                        Logger.Log($"RX ERROR {period}: {ex.Message}");
                    },
                    onCompleted: () =>
                    {
                        Logger.Log($"RX complete {period}");
                        analysisActor.Tell(new ComputeTopics(period));
                    }
                );
        }
    }
}
