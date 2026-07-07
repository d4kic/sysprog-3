using ny_times_most_popular.src.Models;

namespace ny_times_most_popular.src.Actors
{
    public record LoadArticles(int period);
    public record StoreArticle(int period, Article article);
    public record ComputeTopics(int period);
    public record TopicInfo(int clusterId, List<string> reci, int brojClanaka, List<string> naslovi);
    public record TopicsResult(int period, List<TopicInfo> topics, int brojClanaka);
}
