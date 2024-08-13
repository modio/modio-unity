using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace ModIO.Implementation.API
{
    /// <summary>
    /// A WebRequestRunner that can simulate some different failure conditions such as offline status and interrupted downloads
    /// </summary>
    internal class TestWebRequestRunner : IWebRequestRunner
    {
        // Set this to true to cause all requests to timeout with failure after 1.5s
        internal bool TestReturnFailedToConnect = false;

        // Set this to true to cause downloads to show some progress and then fail
        internal bool DownloadsInterruptPartWay = true;

        IWebRequestRunner _fallbackTo = new UnityWebRequestRunner();

        public RequestHandle<Result> Download(string url, Stream downloadTo, ProgressHandle progressHandle)
        {
            if (TestReturnFailedToConnect || DownloadsInterruptPartWay)
            {
                return new RequestHandle<Result>
                {
                    progress = progressHandle,
                    task = DelayAndReturnError(progressHandle),
                    cancel = null,
                };
            }
            return _fallbackTo.Download(url, downloadTo, progressHandle);
        }
        public Task<ResultAnd<TResult>> Execute<TResult>(WebRequestConfig config, RequestHandle<ResultAnd<TResult>> handle, ProgressHandle progressHandle)
        {
            if (TestReturnFailedToConnect)
                return DelayAndReturnError<TResult>(progressHandle);

            return _fallbackTo.Execute(config, handle, progressHandle);
        }
        static async Task<ResultAnd<TResult>> DelayAndReturnError<TResult>(ProgressHandle progressHandle)
        {
            return new ResultAnd<TResult> { result = await DelayAndReturnError(progressHandle) };
        }
        static async Task<Result> DelayAndReturnError(ProgressHandle progressHandle)
        {
            const int millisecondsDelay = 30;
            const int totalDelay = 1500;
            for (int i = 0; i < totalDelay; i += millisecondsDelay)
            {
                await Task.Delay(millisecondsDelay);
                if (progressHandle != null)
                {
                    progressHandle.Progress = 0.8f * (i / (float)totalDelay);
                    progressHandle.BytesPerSecond = Random.Range(150_000, 300_000);
                }
            }
            Debug.LogWarning("TestWebRequestRunner is simulating a network failure");
            return ResultBuilder.Create(ResultCode.API_FailedToConnect);
        }
    }
}
