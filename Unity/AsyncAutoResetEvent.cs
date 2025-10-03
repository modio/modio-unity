using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Modio.Unity
{
    internal class AsyncAutoResetEvent
    {
        struct Empty { }
        readonly Queue<TaskCompletionSource<Empty>> _signalWaiters = new Queue<TaskCompletionSource<Empty>>();
        bool _signaled;

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            lock (_signalWaiters)
            {
                if (_signaled)
                {
                    _signaled = false;
                    return Task.CompletedTask;
                }

                var tcs = new TaskCompletionSource<Empty>(TaskCreationOptions.RunContinuationsAsynchronously);

                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return tcs.Task;
                }
                        
                _signalWaiters.Enqueue(tcs);
                return tcs.Task;
            }
        }

        public void Set()
        {
            TaskCompletionSource<Empty> toRelease = null;
            lock (_signalWaiters)
            {
                if (_signalWaiters.Count > 0)
                    toRelease = _signalWaiters.Dequeue();
                else
                    _signaled = true;
            }
            toRelease?.TrySetResult(default(Empty));
        }
    }
}
