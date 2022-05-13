using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class AddModfile
    {
        // public struct ResponseSchema
        // {
        //     // (NOTE): mod.io returns a ModfileObject as the schema.
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true, canCacheResponse = false,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.POST };

        public static string URL(ModfileDetails details, byte[] filedata, out WWWForm form)
        {
            // TODO @Steve change filedata to FileStream so we don't double up on memory when
            // using form.AddBinaryData

            // get the id from optional
            long id = details.modId == null ? new ModId(0) : details.modId.Value.id;

            // MD5
            string md5 = IOUtil.GenerateMD5(filedata);

            List<KeyValuePair<string, string>> kvps = new List<KeyValuePair<string, string>>();

            kvps.Add(new KeyValuePair<string, string>("version", details.version));
            kvps.Add(new KeyValuePair<string, string>("changelog", details.changelog));
            kvps.Add(new KeyValuePair<string, string>("filehash", md5));
            kvps.Add(new KeyValuePair<string, string>("metadata_blob", details.metadata));

            form = new WWWForm();

            foreach(var kvp in kvps)
            {
                if(!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    form.AddField(kvp.Key, kvp.Value);
                }
            }

            // Add filedata data
            form.AddBinaryData("filedata", filedata, $"{id}_modfile.zip", null);

            return $"{Settings.server.serverURL}{@"/games/"}"
                   + $"{Settings.server.gameId}{@"/mods/"}{id}{@"/files"}?";
        }
    }
}
