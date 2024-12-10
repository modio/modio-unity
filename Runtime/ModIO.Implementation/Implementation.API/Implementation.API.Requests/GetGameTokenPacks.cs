using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetGameTokenPacks
    {
        [System.Serializable]
        public class ResponseSchema : PaginatedResponse<TokenPackObject> { }

        public static WebRequestConfig Request()
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/monetization/token-packs"}",
                RequestMethodType = "GET",
            };

            return request;
        }
    }
}
