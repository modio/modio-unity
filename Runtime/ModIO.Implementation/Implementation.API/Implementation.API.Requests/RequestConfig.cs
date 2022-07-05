
namespace ModIO.Implementation.API.Requests
{
    internal class RequestConfig
    {
        internal bool canCacheResponse = true;
        internal bool requireAuthToken = false;
        internal WebRequestMethodType requestMethodType = WebRequestMethodType.GET;
        internal WebRequestResponseType requestResponseType = WebRequestResponseType.Text;
        internal bool ignoreTimeout = false;
    }
}
