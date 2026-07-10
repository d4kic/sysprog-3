using Akka.Actor;
using Microsoft.ML;
using ny_times_most_popular.src.Models;

namespace ny_times_most_popular.src.Actors
{
    internal class AnalysisActor : ReceiveActor
    {
        private readonly MLContext ml = new();
        private readonly Dictionary<int, List<Article>> articlesByPeriod = new();
        private readonly Dictionary<int, TopicsResult> topicsByPeriod = new();

        private static readonly HashSet<string> skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the","a","an","and","or","of","to","in","on","for","with","is","are","was",
            "were","it","that","this","as","by","at","from","be","has","have","its",
            "his","her","their","but","not","said","will","new","york","times","after"
        };

        public AnalysisActor()
        {
            Receive<ClearArticles>(msg =>
            {
                articlesByPeriod.Remove(msg.period);
                Logger.Log($"Obrisani clanci za period = {msg.period}");
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

            Receive<ComputeTopics>(HandleComputeTopics);
            Receive<StoreArticle>(HandleStoreArticle);
        }

        private void HandleStoreArticle(StoreArticle msg)
        {
            if (!articlesByPeriod.ContainsKey(msg.period))
            {
                articlesByPeriod[msg.period] = new List<Article>();
            }

            articlesByPeriod[msg.period].Add(msg.article);
        }

        private void HandleComputeTopics(ComputeTopics msg)
        {
            if (!articlesByPeriod.TryGetValue(msg.period, out var articles))
            {
                Logger.Log($"Nema clanaka za period = {msg.period}");
                return;
            }

            try
            {
                Logger.Log($"ML.NET analiza za period = {msg.period}");
                var topics = ExtractTopics(articles);
                topicsByPeriod[msg.period] = new TopicsResult(msg.period, topics, articles.Count);
                Logger.Log($"Teme obradjene za period = {msg.period}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Greska u obradi: {ex.Message}");
            }
        }

        private List<TopicInfo> ExtractTopics(List<Article> articles)
        {
            if (articles.Count == 0)
                return new List<TopicInfo>();

            var data = ml.Data.LoadFromEnumerable(articles.Select(a => new TextData
            {
                Text = $"{a.Title} {a.Abstract}"
            }));

            var pipeline = ml.Transforms.Text.FeaturizeText("Features", nameof(TextData.Text))
                .Append(ml.Clustering.Trainers.KMeans(featureColumnName: "Features", numberOfClusters: 5));

            var model = pipeline.Fit(data);
            var transformed = model.Transform(data);
            var predictions = ml.Data.CreateEnumerable<Prediction>(transformed, false).ToList();

            var groups = new Dictionary<uint, List<Article>>();
            for (int i=0; i<articles.Count; i++)
            {
                var label = predictions[i].PredictedLabel;
                if (!groups.TryGetValue(label, out var list))
                {
                    list = new List<Article>();
                    groups[label] = list;
                }
                list.Add(articles[i]);
            }
            
            return groups
                .OrderByDescending(g => g.Value.Count)
                .Select(g => new TopicInfo(
                    clusterId: (int)g.Key,
                    reci: ExtractKeywords(g.Value),
                    brojClanaka: g.Value.Count,
                    naslovi: g.Value.Select(a => a.Title).ToList()))
                .ToList();
        }

        private static List<string> ExtractKeywords(List<Article> articles, int n = 6)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var a in articles)
            {
                var words = $"{a.Title} {a.Abstract}".ToLowerInvariant()
                    .Split(['’', '‘', '“', '”', ' ', '.', ',', ':', ';', '!', '?', '"', '\'', '(', ')', '[', ']', '/', '\\', '-', '_'],
                        StringSplitOptions.RemoveEmptyEntries);
                foreach(var w in words)
                {
                    if (w.Length < 4 || skip.Contains(w))
                        continue;
                    counts[w] = counts.GetValueOrDefault(w) + 1;
                }
            }

            return counts
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .Take(n)
                .Select(kv => kv.Key)
                .ToList();
        }
    }
}
 