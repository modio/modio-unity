using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetCurrentUserRatings
    {
        [System.Serializable]

        internal class ResponseSchema : PaginatedResponse<RatingObject> { }

        public static WebRequestConfig Request()
        {
            return Request(null);
        }

        public static WebRequestConfig Request(SearchFilter searchFilter)
        {
            string filter = string.Empty;
            if(searchFilter != null)
            {
                filter = FilterUtil.ConvertToURL(searchFilter);
            }
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/me/ratings"}?{filter}{@"&game_id="}{Settings.server.gameId}",
                RequestMethodType = "GET"
            };

            return request;
        }
    }
}
