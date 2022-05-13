using JetBrains.Annotations;
using ModIO.Implementation.API.Objects;
using Newtonsoft.Json;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetMods
    {
        [System.Serializable]
        internal class ResponseSchema // TODO see PaginatingRequest<T> and then inherit here
        {
            // TODO(@jackson): Investigate other methods of ensuring we deserialize the correct type
            // Having a Required property helps us to ensure we dont parse the wrong Type T when we
            // ProcessResponse in RESTAPI.cs
            [JsonProperty(Required = Required.Always)]
            internal ModObject[] data;

            [JsonProperty]
            internal int result_count;
            [JsonProperty]
            internal int result_offset;
            [JsonProperty]
            internal int result_limit;
            [JsonProperty]
            internal int result_total;
        }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = false, canCacheResponse = true,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.GET };

        public static string URL_Unpaginated([CanBeNull] SearchFilter searchFilter = null)
        {
            // Convert filter into string
            string filter = string.Empty;
            if(searchFilter != null)
            {
                filter = FilterUtil.ConvertToURL(searchFilter);
            }

            return $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods"}?{filter}";
        }

        public static string URL_Paginated([CanBeNull] SearchFilter searchFilter = null)
        {
            // Convert filter into string
            string filter = string.Empty;
            if(searchFilter != null)
            {
                filter = FilterUtil.ConvertToURL(searchFilter);
                filter = FilterUtil.AddPagination(searchFilter, filter);
            }

            return $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods"}?{filter}";
        }
    }
}
