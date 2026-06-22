namespace ny_times_most_popular.src.Server
{
    internal class WorkerPool
    {
        private readonly RequestQueue? queue;
        private readonly List<Task> workers = new();
        private readonly CancellationToken ctoken;

        public WorkerPool(RequestQueue queue, int workerCount, CancellationToken ctoken)
        {
            this.queue = queue;
            this.ctoken = ctoken;

            for (int i =0; i < workerCount; i++)
            {
                workers.Add(Task.Run(WorkerJob));
            }
        }

        private async Task WorkerJob()
        {
        
        }

        public void WaitAll()
        {
            Task.WaitAll(workers.ToArray());
        }
    }
}
