using System.Net;

namespace ny_times_most_popular.src.Server
{
    internal class WebServer
    {
        private readonly HttpListener listener = new();

        public WebServer()
        {
            listener.Prefixes.Add("http://localhost:5050/");
        }

        public async Task StartAsync()
        {
            listener.Start();
            Console.WriteLine($"[{DateTime.Now}] Server radi na http://localhost:5050/");
            Console.WriteLine("Dugme Z za shutdown...");

            new Thread(() =>
            {
                while (Console.ReadKey(true).Key != ConsoleKey.Z) { }
                Stop();
            })
            {
                IsBackground = true
            }.Start();

            while (true)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    _ = Task.Run(async () =>
                    {
                        await RequestHandler.HandleRequestAsync(context);
                    });
                }
                catch
                {
                    break;
                }
            }
        }

        private void Stop()
        {
            listener.Stop();
            RequestHandler.Stop();
            Logger.Log("Server uspesno ugasen.");
        }
    }
}