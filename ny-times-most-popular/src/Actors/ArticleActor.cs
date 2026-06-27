using Akka.Actor;
using ny_times_most_popular.src.Models;

namespace ny_times_most_popular.src.Actors
{
    internal class ArticleActor : ReceiveActor
    {
        private readonly List<Article> articles = new();

        public ArticleActor()
        {
            Receive<ArticlesBatch>(msg =>
            {
                articles.AddRange(msg.articles);
                Logger.Log($"Prikupljeni clanci: {articles.Count}");
            });
        }
    }
}
