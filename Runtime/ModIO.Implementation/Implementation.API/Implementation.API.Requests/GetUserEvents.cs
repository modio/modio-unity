using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{

    internal static class GetUserEvents
    {
        [System.Serializable]
        internal class ResponseSchema : PaginatedResponse<UserEventObject> { }

        public static WebRequestConfig Request(string filterUrl = null)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/me/events"}?game_id={Settings.server.gameId}",
                RequestMethodType = "GET"
            };

            if(filterUrl != null)
            {
                request.Url += filterUrl;
            }
            
            

            return request;
        }
    }
}
