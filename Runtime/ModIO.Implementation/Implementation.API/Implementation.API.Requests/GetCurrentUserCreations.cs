using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetCurrentUserCreations
    {
        [System.Serializable]
        internal class ResponseSchema : PaginatedResponse<ModObject> { }
        
        public static WebRequestConfig Request(SearchFilter searchFilter = null)
        {
            string filter = string.Empty;
            if(searchFilter != null)
            {
                filter = FilterUtil.ConvertToURL(searchFilter);
            }

            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/me/mods"}?{filter}{@"&game_id="}{Settings.server.gameId}",
                RequestMethodType = "GET"
            };

            

            return request;
        }
    }
    
}
