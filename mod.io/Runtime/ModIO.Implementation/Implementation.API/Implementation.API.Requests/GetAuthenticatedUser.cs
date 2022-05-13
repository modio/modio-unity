using System;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetAuthenticatedUser
    {
        // public struct ResponseSchema
        // {
        //     // (NOTE): mod.io returns a ModObject as the schema.
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestTemplate Template =
            new RequestTemplate { canCacheResponse = true, requireAuthToken = true,
                                  requestMethodType = WebRequestMethodType.GET,
                                  requestResponseType = WebRequestResponseType.Text };

        public static string URL()
        {
            return $"{Settings.server.serverURL}{@"/me?"}";
        }
    }
}
