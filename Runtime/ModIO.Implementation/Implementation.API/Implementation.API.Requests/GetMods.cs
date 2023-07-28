using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetMods
    {
        [System.Serializable]
        public class ResponseSchema : PaginatedResponse<ModObject> { }

        public static WebRequestConfig RequestPaginated(SearchFilter searchFilter)
        {
            var request = new WebRequestConfig
            {
                Url = PaginatedURL(searchFilter),
                RequestMethodType = "GET"
            };
            
            return request;
        }

        static string Url => $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods"}?";
        public static string UnpaginatedURL(SearchFilter filter) => $"{Url}{FilterUtil.ConvertToURL(filter)}";
        public static string PaginatedURL(SearchFilter filter) => FilterUtil.AddPagination(filter, UnpaginatedURL(filter));
    }
}
