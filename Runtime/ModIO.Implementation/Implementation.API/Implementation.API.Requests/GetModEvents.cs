using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{

    internal static class GetModEvents
    {
        [System.Serializable]
        internal class ResponseSchema : PaginatedResponse<ModEventObject> { }

        public static WebRequestConfig Request(string paginationUrl = null)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/events"}?",
                RequestMethodType = "GET"
            };

            if(paginationUrl != null)
            {
                request.Url += paginationUrl;
            }

            

            return request;
        }
    }
}
