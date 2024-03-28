using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetUserSubscriptions
    {
        [System.Serializable]
        internal class ResponseSchema : PaginatedResponse<ModObject> { }

        public static WebRequestConfig Request(SearchFilter searchFilter = null)
        {
            string filter = searchFilter == null ? "" : FilterUtil.ConvertToURL(searchFilter);
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/me/subscribed?"}game_id={Settings.server.gameId}{filter}",
                RequestMethodType = "GET"
            };



            return request;
        }
    }
}
