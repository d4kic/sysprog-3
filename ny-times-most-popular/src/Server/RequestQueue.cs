using System.Net;

namespace ny_times_most_popular.src.Server
{
    internal class RequestQueue
    {
        private readonly Queue<HttpListenerContext> queue = new();
        private readonly object lockObj = new();
        private readonly SemaphoreSlim sem = new SemaphoreSlim(0);

        public void EnqueueRequest(HttpListenerContext context)
        {
            lock (lockObj)
            {
                queue.Enqueue(context);
            }
            sem.Release();
        }

        public async Task<HttpListenerContext?> DequeueRequestAsync(CancellationToken ctoken)
        {
            try
            {
                await sem.WaitAsync(ctoken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            lock (lockObj)
            {
                return queue.Count > 0 ? queue.Dequeue() : null;
            }
        }
    }
}
