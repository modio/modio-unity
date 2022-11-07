using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class DeleteModTags
    {
        // public struct ResponseSchema
        // {
        //     // (NOTE): mod.io returns a ModObject as the schema.
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestConfig Template =
            new RequestConfig { requireAuthToken = true, canCacheResponse = false,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.DELETE };

        public static string URL(ModId modId, string[] tags, out WWWForm form)
        {
            form = new WWWForm();

            foreach(var tag in tags)
            {
                if(!string.IsNullOrWhiteSpace(tag))
                {
                    form.AddField("tags[]", tag);
                }
            }

            return $"{Settings.server.serverURL}{@"/games/"}"
                   + $"{Settings.server.gameId}{@"/mods/"}{(long)modId}/tags?";
        }
    }
}
