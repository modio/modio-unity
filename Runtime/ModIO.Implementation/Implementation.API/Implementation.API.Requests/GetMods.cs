using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetMods
    {
        public class ResponseSchema : PaginatedResponse<ModObject> { }

        public static WebRequestConfig RequestPaginated(SearchFilter searchFilter)
        {
            string filter = string.Empty;
            if(searchFilter != null)
            {
                filter = FilterUtil.ConvertToURL(searchFilter);
                filter = FilterUtil.AddPagination(searchFilter, filter);
            }

            var request = RequestUnpaginated();
            request.Url += filter;


            return request;
        }

        public static WebRequestConfig RequestUnpaginated()
        {
            var request = new WebRequestConfig()
            {
                Url = UnpaginatedURL(),
                RequestMethodType = "GET"
            };

            return request;
        }

        public static string UnpaginatedURL() => $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods"}?";
    }
}
