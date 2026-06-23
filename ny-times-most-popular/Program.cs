using DotNetEnv;
using ny_times_most_popular.src.Server;

Env.Load();

WebServer server = new WebServer();
await server.StartAsync();