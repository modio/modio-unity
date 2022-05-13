using System;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class AddModRating
    {
        // public struct ResponseSchema
        // {
        //     // (NOTE): mod.io returns a MessageObject as the schema.
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true, canCacheResponse = false,
                                  requestMethodType = WebRequestMethodType.POST,
                                  requestResponseType = WebRequestResponseType.Text };

        public static string URL(ModId modId, ModRating rating, out WWWForm form)
        {
            form = new WWWForm();
            form.AddField("rating", ((int)rating).ToString());

            return $"{Settings.server.serverURL}{@"/games/"}"
                   + $"{Settings.server.gameId}{@"/mods/"}{modId.id}{@"/ratings"}?";
        }
    }
}
