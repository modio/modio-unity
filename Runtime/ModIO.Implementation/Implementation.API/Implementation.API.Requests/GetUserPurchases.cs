using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetUserPurchases
    {
        [System.Serializable]
        public class ResponseSchema : PaginatedResponse<ModObject> { }

        public static WebRequestConfig Request(SearchFilter searchFilter = null)
        {
            string urlWithFilter = searchFilter == null ? Url: PaginatedURL(searchFilter);
            var request = new WebRequestConfig
            {
                //https://api.mod.io/v1/me/purchased?
                Url = urlWithFilter,
                RequestMethodType = "GET"
            };

            return request;
        }

        static string Url => $"{Settings.server.serverURL}{@"/me/purchased?&game_id="}{Settings.server.gameId}";
        public static string UnpaginatedURL(SearchFilter filter) => $"{Url}{FilterUtil.ConvertToURL(filter)}";
        public static string PaginatedURL(SearchFilter filter) => FilterUtil.AddPagination(filter, UnpaginatedURL(filter));
    }
}
