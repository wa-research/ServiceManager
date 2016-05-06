using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FolderMonitor
{
    /// <summary>
    /// Producer-consumer class from http://www.albahari.com/nutshell/cs4ch22.aspx
    /// </summary>
    public class ProducerConsumerQueue : IDisposable
    {
        BlockingCollection<WorkItem> _taskQ = new BlockingCollection<WorkItem>();

        public ProducerConsumerQueue(int workerCount)
        {
            // Create and start a separate Task for each consumer:
            for (int i = 0; i < workerCount; i++)
                Task.Factory.StartNew(Consume);
        }

        public void Dispose() { _taskQ.CompleteAdding(); }

        public Task EnqueueTask(Action action)
        {
            return EnqueueTask(action, null);
        }

        public Task EnqueueTask(Action action, CancellationToken? cancelToken)
        {
            var tcs = new TaskCompletionSource<object>();
            _taskQ.Add(new WorkItem(tcs, action, cancelToken));
            return tcs.Task;
        }

        public int Length
        {
            get { return _taskQ.Count; }
        }

        void Consume()
        {
            foreach (WorkItem workItem in _taskQ.GetConsumingEnumerable())
                if (workItem.CancelToken.HasValue &&
                    workItem.CancelToken.Value.IsCancellationRequested) {
                    workItem.TaskSource.SetCanceled();
                } else
                    try {
                        workItem.Action();
                        workItem.TaskSource.SetResult(null);   // Indicate completion
                    } catch (Exception ex) {
                        workItem.TaskSource.SetException(ex);
                    }
        }

        class WorkItem
        {
            public readonly TaskCompletionSource<object> TaskSource;
            public readonly Action Action;
            public readonly CancellationToken? CancelToken;

            public WorkItem(
              TaskCompletionSource<object> taskSource,
              Action action,
              CancellationToken? cancelToken)
            {
                TaskSource = taskSource;
                Action = action;
                CancelToken = cancelToken;
            }
        }
    }
}