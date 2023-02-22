using System;

namespace ModIO.Implementation.API.Requests
{
    internal static class UserUnmute
    {
        [Obsolete("No response object is given")]
        public struct ResponseSchema
        {
            // (NOTE): no response object is given, just a 204 for success
        }

        public static readonly RequestConfig Template =
            new RequestConfig { requireAuthToken = true, canCacheResponse = false,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.DELETE };

        public static string URL(long userId)
        {
            return $"{Settings.server.serverURL}{@"/users/"}{userId}{@"/mute"}?";
        }
    }
}
