using ModIO.Implementation.API.Objects;
using Newtonsoft.Json;

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

        public static WebRequestConfig Request(long modId)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}/games/{Settings.server.gameId}/mods/{modId}/dependencies?",
                RequestMethodType = "GET"
            };

            

            return request;
        }
    }
}
