namespace ModIO.Implementation.API.Requests
{
    internal static class DownloadImage
    {
        // (NOTE): returns a Texture as the schema.

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true,
                                  requestMethodType = WebRequestMethodType.GET,
                                  requestResponseType = WebRequestResponseType.Texture };
    }
}
