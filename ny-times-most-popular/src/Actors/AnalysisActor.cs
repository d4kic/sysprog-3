using Akka.Actor;
using Microsoft.ML;
using ny_times_most_popular.src.Models;

namespace ny_times_most_popular.src.Actors
{
    internal class AnalysisActor : ReceiveActor
    {
        private readonly MLContext ml = new();

        public AnalysisActor()
        {
            Receive<List<Article>>(articles =>
            {
                var topics = ExtractTopics(articles);

                Sender.Tell(new TopicsResult(topics));
            });
        }

        private List<string> ExtractTopics(List<Article> articles)
        {
            if (articles.Count == 0)
                return ["No data"];

            var data = ml.Data.LoadFromEnumerable(articles.Select(a => new TextData
            {
                Text = $"{a.Title} {a.Abstract}"
            }));

            var pipeline = ml.Transforms.Text.FeaturizeText("Features", nameof(TextData.Text))
                .Append(ml.Clustering.Trainers.KMeans(featureColumnName: "Features", numberOfClusters: 5));

            var model = pipeline.Fit(data);

            var transformed = model.Transform(data);

            var predictions = ml.Data.CreateEnumerable<Prediction>(transformed, false);

            return predictions.GroupBy(x => x.PredictedLabel)
                .OrderByDescending(g => g.Count())
                .Select(g => $"Topic: {g.Key}")
                .ToList();
        }
    }
}
