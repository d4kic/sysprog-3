using DotNetEnv;
using ny_times_most_popular.src.Server;

Env.Load();
Console.WriteLine(Environment.GetEnvironmentVariable("API_KEY"));
WebServer server = new WebServer();
await server.StartAsync();