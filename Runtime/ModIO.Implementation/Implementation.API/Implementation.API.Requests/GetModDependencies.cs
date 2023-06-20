using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetModDependencies
    {
        [System.Serializable]
        internal class ResponseSchema : PaginatedResponse<ModDependenciesObject> { }

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
