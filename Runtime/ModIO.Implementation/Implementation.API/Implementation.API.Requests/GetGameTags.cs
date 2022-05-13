using ModIO.Implementation.API.Objects;
using Newtonsoft.Json;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetGameTags
    {
        [System.Serializable]
        public struct ResponseSchema
        {
            [JsonProperty(Required = Required.Always)]
            internal GameTagOptionObject[] data;

            [JsonProperty]
            public int result_count;

            [JsonProperty]
            public int result_offset;

            [JsonProperty]
            public int result_limit;

            [JsonProperty]
            public int result_total;
        }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = false,
                                  requestMethodType = WebRequestMethodType.GET };

        public static string URL()
        {
            return $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/tags"}?";
        }
    }
}
