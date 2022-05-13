using System;

namespace ModIO.Implementation.API.Requests
{
    internal static class DeleteMod
    {
        [Obsolete("No response object is given")]
        public struct ResponseSchema
        {
            // (NOTE): no response object is given, just a 204 for success
        }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true, canCacheResponse = false,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.DELETE };

        public static string URL(ModId modId)
        {
            return $"{Settings.server.serverURL}{@"/games/"}"
                   + $"{Settings.server.gameId}{@"/mods/"}{modId.id.ToString()}?";
        }
    }
}
