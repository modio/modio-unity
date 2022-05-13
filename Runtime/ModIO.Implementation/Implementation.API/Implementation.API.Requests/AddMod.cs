using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class AddMod
    {
        // public struct ResponseSchema
        // {
        //     // (NOTE): mod.io returns a ModObject as the schema.
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true, canCacheResponse = false,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.POST };

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
            kvps.Add(new KeyValuePair<string, string>("maturity_option",
                                                      details.contentWarning.ToString()));
            kvps.Add(new KeyValuePair<string, string>("metadata_blob", details.metadata));

            if(details.tags != null)
            {
                int count = 0;
                foreach(string tag in details.tags)
                {
                    kvps.Add(new KeyValuePair<string, string>($"tags[{count}]", tag));
                    count++;
                }
            }

            form = new WWWForm();

            foreach(var kvp in kvps)
            {
                if(!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    form.AddField(kvp.Key, kvp.Value);
                }
            }

            // Add logo
            if(details.logo != null)
            {
                form.AddBinaryData("logo", details.logo.EncodeToPNG(), "logo.png", null);
            }

            return $"{Settings.server.serverURL}{@"/games/"}"
                   + $"{Settings.server.gameId}{@"/mods"}?";
        }
    }
}
