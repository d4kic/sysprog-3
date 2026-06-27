using ny_times_most_popular.src.Models;

namespace ny_times_most_popular.src.Actors
{
    public record LoadArticles(int period);
    public record ArticlesBatch(List<Article> articles);
    public record ComputeTopics(int period, List<Article> articles);
    public record TopicInfo(int clusterId, List<string> keywords, int articleCount, List<string> sampleTitles);
    public record TopicsResult(int period, List<string> topics, int articleCount);
}
