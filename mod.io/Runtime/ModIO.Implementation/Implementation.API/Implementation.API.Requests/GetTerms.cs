using System;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetTerms
    {
        // public struct ResponseSchema
        // {
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestTemplate Template =
            new RequestTemplate { canCacheResponse = true, requireAuthToken = false,
                                  requestMethodType = WebRequestMethodType.GET,
                                  requestResponseType = WebRequestResponseType.Text };

        public static string URL()
        {
            string url = $"{Settings.server.serverURL}{@"/authenticate/terms"}?";

            return url;
        }
    }
}
