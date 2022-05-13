using JetBrains.Annotations;
using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetUserSubscriptions
    {
        [System.Serializable]
        internal class ResponseSchema : PaginatingRequest<ModObject>
        {
        }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true, canCacheResponse = true,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.GET };

        public static string URL([CanBeNull] SearchFilter searchFilter = null)
        {
            // Convert filter into string
            string filter =
                searchFilter == null ? "" : FilterUtil.ConvertToURL(searchFilter);

            return $"{Settings.server.serverURL}{@"/me/subscribed?"}game_id={Settings.server.gameId}{filter}";
        }
    }
}
