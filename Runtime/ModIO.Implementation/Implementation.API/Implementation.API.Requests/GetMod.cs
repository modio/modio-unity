using System;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetMod
    {
        // public struct ResponseSchema
        // {
        //     // (NOTE): mod.io returns a ModObject as the schema.
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = false, canCacheResponse = true,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.GET };

        public static string URL(long modId)
        {
            return $"{Settings.server.serverURL}{@"/games/"}"
                   + $"{Settings.server.gameId}{@"/mods/"}{modId}?";
        }
    }
}
