using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{

    internal static class GetGame
    {
        public static WebRequestConfig<ModObject> Request()
        {
            return new WebRequestConfig<ModObject>()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}?",
                RequestMethodType = "GET"
            };
        }
    }
}
