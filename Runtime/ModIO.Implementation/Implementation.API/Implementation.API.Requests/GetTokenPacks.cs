using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetTokenPacks
    {
        [System.Serializable]
        public class ResponseSchema : PaginatedResponse<TokenPackObject> { }

        public static WebRequestConfig Request() => new WebRequestConfig
        {
            Url = $"{Settings.server.serverURL}/games/{Settings.server.gameId}/monetization/token-packs",
            RequestMethodType = "GET",
        };
    }
}
