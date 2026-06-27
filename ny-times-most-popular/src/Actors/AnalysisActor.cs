using Akka.Actor;
using Microsoft.ML;
using ny_times_most_popular.src.Models;
using System.Linq;

namespace ny_times_most_popular.src.Actors
{
    internal class AnalysisActor : ReceiveActor
    {
        private readonly MLContext ml = new();

        public AnalysisActor()
        {
            Receive<ComputeTopics>(msg =>
            {
                var topics = ExtractTopics(msg.articles);
                Sender.Tell(new TopicsResult(msg.period, topics, msg.articles.Count));
            });
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
                    keywords: ExtractKeywords(g.Value),
                    articleCount: g.Value.Count,
                    sampleTitles: g.Value.Take(3).Select(a => a.Title).ToList()))
                .ToList();
        }

        private static List<string> ExtractKeywords(List<Article> articles, int n = 6)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var a in articles)
            {
                var words = $"{a.Title} {a.Abstract}".ToLowerInvariant()
                    .Split(new[] { ' ', '.', ',', ':', ';', '!', '?', '"', '\'', '(', ')', '[', ']', '/', '\\', '-', '_' },
                        StringSplitOptions.RemoveEmptyEntries);
                foreach(var w in words)
                {
                    if (w.Length < 4)
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
 