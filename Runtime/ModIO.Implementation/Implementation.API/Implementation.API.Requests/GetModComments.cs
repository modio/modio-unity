using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetModComments
    {
        public class ResponseSchema : PaginatedResponse<ModCommentObject> { }

        public static WebRequestConfig RequestPaginated(long modId, SearchFilter searchFilter)
        {
            var request = new WebRequestConfig
            {
                Url = PaginatedURL(modId, searchFilter),
                RequestMethodType = "GET"
            };

            return request;
        }

        static string Url(long modId) => $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}/comments?";
        public static string UnpaginatedURL(long modId, SearchFilter filter) => $"{Url(modId)}{FilterUtil.ConvertToURL(filter)}";
        public static string PaginatedURL(long modId, SearchFilter filter) => FilterUtil.AddPagination(filter, UnpaginatedURL(modId, filter));
    }
}
