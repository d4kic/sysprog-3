using Akka.Actor;
using ny_times_most_popular.src.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ny_times_most_popular.src.Actors
{
    internal class ManagerActor: ReceiveActor
    {
        private readonly Dictionary<int, IActorRef> actors = new();
        private readonly Dictionary<int, List<Article>> articlesByPeriod = new();
        private readonly Dictionary<int, TopicsResult> topicsByPeriod = new();

        public ManagerActor()
        {
            Receive<ClearArticles>(msg =>
            {
                articlesByPeriod[msg.period] = new List<Article>();
                Logger.Log($"Obrisani clanci za period = {msg.period}");
            });

            Receive<StoreArticle>(msg =>
            {
                if (!articlesByPeriod.ContainsKey(msg.period))
                {
                    articlesByPeriod[msg.period] = new List<Article>();
                }
                articlesByPeriod[msg.period].Add(msg.article);
            });

            Receive<ComputeTopics>(msg =>
            {
                if (!articlesByPeriod.TryGetValue(msg.period, out var articles))
                {
                    Logger.Log($"Nema clanaka za period = {msg.period}");
                    return;
                }

                var actor = ActorForPeriod(msg.period);
                actor.Ask<TopicsResult>(new AnalyzeArticles(msg.period, articles.ToList()), TimeSpan.FromSeconds(10))
                    .PipeTo(Self);
            });

            Receive<TopicsResult>(res =>
            {
                topicsByPeriod[res.period] = res;
                Logger.Log($"Sacuvane teme za period = {res.period}");
            });

            Receive<GetTopics>(msg =>
            {
                if (topicsByPeriod.TryGetValue(msg.period, out var result))
                {
                    Sender.Tell(result);
                }
                else
                {
                    Sender.Tell(new TopicsResult(msg.period, new List<TopicInfo>(), 0));
                }
            });
        }

        private IActorRef ActorForPeriod(int period)
        {
            if (!actors.TryGetValue(period, out var actor))
            {
                actor = Context.ActorOf(
                    AnalysisActor.Props().WithDispatcher("analysis-disp"), $"analysis-{period}");
                actors[period] = actor;
                Logger.Log($"Kreiran aktor za period = {period}");
            }
            return actor;
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create<ManagerActor>();
        }
    }
}
