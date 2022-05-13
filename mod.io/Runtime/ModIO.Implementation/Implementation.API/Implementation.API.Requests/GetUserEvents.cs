using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetUserEvents
    {
        [System.Serializable]
        internal class ResponseSchema : PaginatingRequest<UserEventObject>
        {
        }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true, canCacheResponse = false,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.GET };

        public static string URL()
        {
            return $"{Settings.server.serverURL}{@"/me/events"}?game_id={Settings.server.gameId}";
        }
    }
}
