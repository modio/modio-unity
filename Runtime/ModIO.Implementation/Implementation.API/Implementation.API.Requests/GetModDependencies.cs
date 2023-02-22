using JetBrains.Annotations;
using ModIO.Implementation.API.Objects;
using Newtonsoft.Json;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetModDependencies
    {
        [System.Serializable]
        internal class ResponseSchema
        {
            [JsonProperty(Required = Required.Always)]
            internal ModDependenciesObject[] data;

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
        public static string Url(ModId modId)
        {
            //GET https://api.mod.io/v1/games/{game-id}/mods/{mod-id}/dependencies?api_key=YourApiKey HTTP/1.1
            return $"{Settings.server.serverURL}/games/{Settings.server.gameId}/mods/{modId.id}/dependencies?";
        }
    }
}
