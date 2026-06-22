using ny_times_most_popular.src.Models;
using System.Reactive.Linq;
using System.Text.Json;

namespace ny_times_most_popular.src.Server
{
    internal class NytService
    {
        private readonly HttpClient client = new();
        private readonly string key;

        public NytService(string key)
        {
            this.key = key;
        }

        public IObservable<Article> GetArticles(int period)
        {
            return Observable.FromAsync(async () =>
            {
                var url = $"https://api.nytimes.com/svc/mostpopular/v2/viewed/{period}.json?api-key={key}";
                return await client.GetStringAsync(url);
            })
            .SelectMany(json =>
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement
                    .GetProperty("results")
                    .EnumerateArray()
                    .Select(x => new Article
                    {
                        Title = x.GetProperty("title").GetString() ?? "",
                        Abstract = x.GetProperty("abstract").GetString() ?? "",
                        Url = x.GetProperty("url").GetString() ?? ""
                    });
            })
            .Where(a => !string.IsNullOrEmpty(a.Title));
        }
    }
}
