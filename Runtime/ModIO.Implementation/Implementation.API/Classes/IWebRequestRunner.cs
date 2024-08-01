using System.IO;
using System.Threading.Tasks;

namespace ModIO.Implementation.API
{
    interface IWebRequestRunner
    {
        RequestHandle<Result> Download(string url, Stream downloadTo, ProgressHandle progressHandle);
        Task<ResultAnd<TResult>> Execute<TResult>(WebRequestConfig config, RequestHandle<ResultAnd<TResult>> handle, ProgressHandle progressHandle);
    }
}
