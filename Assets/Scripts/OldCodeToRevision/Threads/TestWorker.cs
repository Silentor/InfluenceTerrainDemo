using System.Threading;

namespace TerrainDemo.OldCodeToRevision.Threads
{
    public class TestWorker : WorkerPool.WorkerBase<int, int>
    {
        protected override int WorkerLogic(int data)
        {
            Thread.Sleep(data);
            return data*2;
        }
    }
}
