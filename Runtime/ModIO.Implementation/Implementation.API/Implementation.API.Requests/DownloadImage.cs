namespace ModIO.Implementation.API.Requests
{
    internal static class DownloadImage
    {
        // (NOTE): returns a Texture as the schema.

        public static readonly RequestConfig Template =
            new RequestConfig { requireAuthToken = true,
                                  requestMethodType = WebRequestMethodType.GET,
                                  requestResponseType = WebRequestResponseType.Texture };
    }
}
