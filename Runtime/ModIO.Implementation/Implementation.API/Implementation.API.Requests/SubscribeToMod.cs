using System;

namespace ModIO.Implementation.API.Requests
{
    internal static class SubscribeToMod
    {
        // public struct ResponseSchema
        // {
        //     // (NOTE): mod.io returns a ModObject as the schema.
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true, canCacheResponse = false,
                                  requestMethodType = WebRequestMethodType.POST };

        public static string URL(long modId)
        {
            string url = $"{Settings.server.serverURL}{@"/games/"}"
                         + $"{Settings.server.gameId}{@"/mods/"}{modId}{@"/subscribe"}?";

            return url;
        }
    }
}
