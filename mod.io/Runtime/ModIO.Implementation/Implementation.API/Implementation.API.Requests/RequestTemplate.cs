
namespace ModIO.Implementation.API.Requests
{
    internal class RequestTemplate
    {
        internal bool canCacheResponse = true;
        internal bool requireAuthToken = false;
        internal WebRequestMethodType requestMethodType = WebRequestMethodType.GET;
        internal WebRequestResponseType requestResponseType = WebRequestResponseType.Text;
    }
}
