namespace ModIO.Implementation.API.Requests
{

    internal static class DeleteModComment
    {
        public static WebRequestConfig Request(ModId modId, long commentId)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{(long)modId}/comments/{commentId}",
                RequestMethodType = "DELETE",
            };

            return request;
        }
    }
}
