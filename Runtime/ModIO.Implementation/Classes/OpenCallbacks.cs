using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModIO.Implementation
{
    class OpenCallbacks
    {
        Dictionary<TaskCompletionSource<bool>, Task> openCallbacks =
            new Dictionary<TaskCompletionSource<bool>, Task>();


        public TaskCompletionSource<bool> New()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            openCallbacks.Add(tcs, null);
            return tcs;
        }

        public TaskCompletionSource<bool> New(Task task)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            openCallbacks.Add(tcs, task);
            return tcs;
        }

        public async Task<T> Run<T>(TaskCompletionSource<bool> tcs, Task<T> task)
        {
            openCallbacks[tcs] = task;
            var response = await task;
            openCallbacks[tcs] = null;
            
            return response;
        }

        public void Remove(TaskCompletionSource<bool> tcs)
        {
            openCallbacks.Remove(tcs);
        }

        public void Complete(TaskCompletionSource<bool> tcs)
        {
            tcs.SetResult(true);
            openCallbacks.Remove(tcs);
        }

        public void Clear(TaskCompletionSource<bool> tcs)
        {
            openCallbacks[tcs] = null;
        }

        public void CancelAll()
        {
            foreach(var kvp in openCallbacks)
            {
                kvp.Key.TrySetCanceled();
            }
            openCallbacks.Clear();
        }
        
        public async Task ShutDown()
        {
            // get new instance of dictionary so it's thread safe
            Dictionary<TaskCompletionSource<bool>, Task> tasks =
                new Dictionary<TaskCompletionSource<bool>, Task>(openCallbacks);

            // iterate over the tasks and await for non faulted callbacks to finish
            using(var enumerator = tasks.GetEnumerator())
            {
                while(enumerator.MoveNext())
                {
                    if(enumerator.Current.Value == null 
                       || enumerator.Current.Value.IsFaulted
                       || enumerator.Current.Value.IsCanceled)
                    {
                        Logger.Log(LogLevel.Warning,
                            "Invalid TCS stored in openCallbacks."
                            + " The corresponding callback will never be invoked. Skipping this"
                            + " task (The shutdown method may complete before all jobs have "
                            + "been cancelled).");
                        if(openCallbacks.ContainsKey(enumerator.Current.Key))
                        {
                            openCallbacks.Remove(enumerator.Current.Key);
                        }
                    }
                    else
                    {
                        await enumerator.Current.Key.Task;
                    }
                }
            }
            
            Logger.Log(LogLevel.Verbose, "Shutdown finished waiting for callbacks");
        }
    }
}
