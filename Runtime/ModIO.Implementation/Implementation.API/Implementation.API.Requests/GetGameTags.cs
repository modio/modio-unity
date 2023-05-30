using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{

    internal static class GetGameTags
    {
        [System.Serializable]
        public class ResponseSchema : PaginatedResponse<GameTagOptionObject> { }

        public static WebRequestConfig Request()
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/tags"}?show_hidden_tags=true",
                RequestMethodType = "GET"
            };

            

            return request;
        }
    }
}
