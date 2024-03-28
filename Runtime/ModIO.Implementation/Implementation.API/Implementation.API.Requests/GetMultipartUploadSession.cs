namespace ModIO.Implementation.API.Requests
{
    static class GetMultipartUploadSession
    {
        public static WebRequestConfig Request(long modId, SearchFilter filter)
        {
            return new WebRequestConfig()
            {
                Url = FilterUtil.AddPagination(filter, $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}{@"/files/multipart/sessions?"}{FilterUtil.ConvertToURL(filter)}"),
                RequestMethodType = "GET",
            };
        }
    }
}
