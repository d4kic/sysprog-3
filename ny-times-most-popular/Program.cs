using DotNetEnv;
using ny_times_most_popular.src.Server;

Env.Load();

var fetcher = new RxFetcher(
    new NytService(Environment.GetEnvironmentVariable("API_KEY")!),
    RequestHandler.Manager);
fetcher.Start();

WebServer server = new WebServer();
await server.StartAsync();