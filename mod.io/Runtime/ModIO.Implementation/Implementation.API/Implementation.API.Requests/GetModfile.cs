using System;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetModfile
    {
        // public struct ResponseSchema
        // {
        //     // (NOTE): mod.io returns a ModfileObject as the schema.
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true, canCacheResponse = true,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.GET };
    }
}
