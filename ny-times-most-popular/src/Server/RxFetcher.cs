using Akka.Actor;
using ny_times_most_popular.src.Actors;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace ny_times_most_popular.src.Server
{
    internal class RxFetcher
    {
        private readonly NytService service;
        private readonly IActorRef manager;
        private readonly int[] periods = { 1, 7, 30 };

        public RxFetcher(NytService service, IActorRef manager)
        {
            this.service = service;
            this.manager = manager;
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
            manager.Tell(new ClearArticles(period));

            service.GetArticles(period)
                .SubscribeOn(TaskPoolScheduler.Default)
                .Subscribe(
                    onNext: a =>
                    {
                        Logger.Log($"RX {period} obradio: {a.Title}");
                        manager.Tell(new StoreArticle(period, a));
                    },
                    onError: ex =>
                    {
                        Logger.Log($"RX ERROR {period}: {ex.Message}");
                    },
                    onCompleted: () =>
                    {
                        Logger.Log($"RX complete {period}");
                        manager.Tell(new ComputeTopics(period));
                    }
                );
        }
    }
}
