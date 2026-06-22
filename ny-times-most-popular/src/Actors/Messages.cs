using ny_times_most_popular.src.Models;

namespace ny_times_most_popular.src.Actors
{
    public record LoadArticles(int period);
    public record ArticlesBatch(List<Article> Articles);
    public record Analyze();
    public record TopicsResult(List<string> Topics);
}
