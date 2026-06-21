using ny_times_most_popular.src.Server;

WebServer server = new WebServer(4);
await server.StartAsync();