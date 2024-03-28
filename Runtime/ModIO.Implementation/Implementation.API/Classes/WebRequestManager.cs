using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;

namespace ModIO.Implementation.API
{
    /// <summary>
    /// This class keeps track of all ongoing web requests so that a shutdown call will properly
    /// abort and cleanup active requests.
    /// All requests should be generated and run from this script.
    /// </summary>
    static class WebRequestManager
    {
        static Queue<Tuple<string, object>> liveTasks = new Queue<Tuple<string, object>>();
        static HashSet<Task> onGoingRequests = new HashSet<Task>();

        internal static event Action ShutdownEvent = () => { };

        public static async Task Shutdown()
        {
            // Subscribe WebRequests here to abort when the event is called
            ShutdownEvent.Invoke();

            await Task.WhenAll(onGoingRequests);

            ShutdownEvent = null;
            ShutdownEvent = () => { };
        }

        public static RequestHandle<Result> Download(string url, Stream downloadTo, ProgressHandle progressHandle)
        {
            var handle = WebRequestRunner.Download(url, downloadTo, progressHandle);
            onGoingRequests.Add(handle.task);
            RemoveTaskFromListWhenComplete(handle.task);
            return handle;
        }

        static async void RemoveTaskFromListWhenComplete(Task task)
        {
            await task;
            if(onGoingRequests.Contains(task))
            {
                onGoingRequests.Remove(task);
            }
        }

        public static async Task<ResultAnd<TOutput>> Request<TOutput>(WebRequestConfig config, ProgressHandle progressHandle = null)
        {
            if(!PreexistingGetRequest(config, out Task<ResultAnd<TOutput>> task))
            {
                task = NewRequest<TOutput>(config, progressHandle);
                onGoingRequests.Add(task);
            }

            await task;
            
            if(onGoingRequests.Contains(task))
            {
                onGoingRequests.Remove(task);
            }

            return task.Result;
        }

        //Some requests do not need an answer outside of "success".
        public static async Task<Result> Request(WebRequestConfig config)
        {
            //we use int? to signify that we don't care about the result
            //a webrequest is not capabable of returning this value, so it's a
            //suitable placeholder
            var task = await Request<int?>(config);
            return task.result;
        }

        static Task<ResultAnd<TOutput>> NewRequest<TOutput>(WebRequestConfig config, ProgressHandle progressHandle = null)
            =>  WebRequestRunner.Execute<TOutput>(config, null, progressHandle);

        static bool PreexistingGetRequest<TOutput>(WebRequestConfig config, out Task<ResultAnd<TOutput>> task)
        {
            task = null;
            if(config.RequestMethodType != "GET")
            {
                return false;
            }

            foreach(var t in liveTasks)
            {
                if(t.Item1 == config.Url)
                {
                    task = (Task<ResultAnd<TOutput>>)t.Item2;
                    return true;
                }
            }

            return false;
        }
    }
}
