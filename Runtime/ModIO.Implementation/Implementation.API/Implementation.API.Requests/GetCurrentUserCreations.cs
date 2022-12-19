
using JetBrains.Annotations;
using ModIO.Implementation.API.Objects;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetCurrentUserCreations
    {
        [System.Serializable]
        internal class ResponseSchema : PaginatingRequest<ModObject> { }

        public static readonly RequestConfig Template =
            new RequestConfig { requireAuthToken = true, canCacheResponse = true,
                requestResponseType = WebRequestResponseType.Text,
                requestMethodType = WebRequestMethodType.GET };
        public static string Url([CanBeNull] SearchFilter searchFilter = null)
        {
            // Convert filter into string
            string filter = string.Empty;
            if(searchFilter != null)
            {
                filter = FilterUtil.ConvertToURL(searchFilter);
            }

            return $"{Settings.server.serverURL}{@"/me/mods"}?{filter}{@"&game_id="}{Settings.server.gameId}";
        }
    }
}
