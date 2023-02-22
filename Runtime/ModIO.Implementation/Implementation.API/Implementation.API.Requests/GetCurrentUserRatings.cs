
using JetBrains.Annotations;
using ModIO.Implementation.API.Objects;
using Newtonsoft.Json;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetCurrentUserRatings
    {
        [System.Serializable]
        internal class ResponseSchema
        {
            [JsonProperty(Required = Required.Always)]
            internal RatingObject[] data;

            [JsonProperty]
            internal int result_count;
            [JsonProperty]
            internal int result_offset;
            [JsonProperty]
            internal int result_limit;
            [JsonProperty]
            internal int result_total;
        }

        public static readonly RequestConfig Template =
            new RequestConfig { requireAuthToken = true, canCacheResponse = true,
                requestResponseType = WebRequestResponseType.Text,
                requestMethodType = WebRequestMethodType.GET };

        public static string Url()
        {
            //GET https://api.mod.io/v1/me/ratings HTTP/1.1
            return $"{Settings.server.serverURL}{@"/me/ratings"}?";
        }
    }
}
