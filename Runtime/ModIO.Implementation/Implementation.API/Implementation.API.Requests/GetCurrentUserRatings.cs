using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetCurrentUserRatings
    {
        [System.Serializable]

        internal class ResponseSchema : PaginatedResponse<RatingObject> { }

        public static WebRequestConfig Request()
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/me/ratings"}?",
                RequestMethodType = "GET"
            };

            return request;
        }
    }
}
