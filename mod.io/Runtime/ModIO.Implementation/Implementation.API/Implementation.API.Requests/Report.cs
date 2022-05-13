using System;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class Report
    {
        // public struct ResponseSchema
        // {
        //     // (NOTE): mod.io returns a MessageObject as the schema.
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = false, canCacheResponse = true,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.POST };

        public static string URL(ModIO.Report report, out WWWForm form)
        {
            string url = $"{Settings.server.serverURL}{@"/report"}?";

            form = new WWWForm();

            // id
            form.AddField("id", report.id.ToString());

            // resource
            switch(report.resourceType)
            {
                case ReportResourceType.Games:
                    form.AddField("resource", "games");
                    break;
                case ReportResourceType.Mods:
                    form.AddField("resource", "mods");
                    break;
                case ReportResourceType.Users:
                    form.AddField("resource", "users");
                    break;
            }

            // type
            form.AddField("type", ((int)report.type).ToString());

            // name
            form.AddField("name", report.user);

            // contact
            form.AddField("contact", report.contactEmail);

            // summary
            form.AddField("summary", report.summary);

            return url;
        }
    }
}
