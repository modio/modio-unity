namespace ModIO.Implementation.API.Requests
{
    internal static class DownloadBinary
    {
        // (NOTE): This request does not use a response schema

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true, canCacheResponse = false,
                                  requestResponseType = WebRequestResponseType.File,
                                  requestMethodType = WebRequestMethodType.GET };
    }
}
