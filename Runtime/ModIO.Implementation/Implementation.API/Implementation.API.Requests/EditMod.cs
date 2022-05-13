using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class EditMod
    {
        // public struct ResponseSchema
        // {
        //     // (NOTE): mod.io returns a ModObject as the schema.
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true, canCacheResponse = false,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.PUT };

        public static string URL(ModProfileDetails details, out WWWForm form)
        {
            List<KeyValuePair<string, string>> kvps = new List<KeyValuePair<string, string>>();

            kvps.Add(
                new KeyValuePair<string, string>("visible", details.visible == false ? "0" : "1"));
            kvps.Add(new KeyValuePair<string, string>("name", details.name));
            kvps.Add(new KeyValuePair<string, string>("summary", details.summary));
            kvps.Add(new KeyValuePair<string, string>("description", details.description));
            kvps.Add(new KeyValuePair<string, string>("name_id", details.name_id));
            kvps.Add(new KeyValuePair<string, string>("homepage_url", details.homepage_url));
            kvps.Add(new KeyValuePair<string, string>("stock", details.maxSubscribers.ToString()));
            if(details.contentWarning != null)
            {
                kvps.Add(new KeyValuePair<string, string>(
                    "maturity_option", ((int)details.contentWarning).ToString()));
            }
            kvps.Add(new KeyValuePair<string, string>("metadata_blob", details.metadata));

            form = new WWWForm();

            foreach(var kvp in kvps)
            {
                if(!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    form.AddField(kvp.Key, kvp.Value);
                }
            }

            // Get the ModId (which is a nullable)
            long modId = 0;
            if(details.modId != null)
            {
                modId = details.modId.Value.id;
            }
            return $"{Settings.server.serverURL}{@"/games/"}"
                   + $"{Settings.server.gameId}{@"/mods/"}{modId}?";
        }
    }
}
