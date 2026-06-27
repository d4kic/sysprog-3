using Akka.Actor;
using ny_times_most_popular.src.Models;

namespace ny_times_most_popular.src.Actors
{
    internal class ArticleActor : ReceiveActor
    {
        private readonly Dictionary<int, List<Article>> articles = new();

        public ArticleActor()
        {
            Receive<ArticlesBatch>(msg =>
            {
                if (!articles.TryGetValue(msg.period, out var list))
                {
                    list = new List<Article>();
                    articles[msg.period] = list;
                }
                list.AddRange(msg.articles);
                Logger.Log($"Prikupljeni clanci: {msg.articles.Count}");
            });
        }
    }
}
