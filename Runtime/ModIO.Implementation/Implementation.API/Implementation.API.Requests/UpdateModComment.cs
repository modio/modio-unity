namespace ModIO.Implementation.API.Requests
{
    internal static class UpdateModComment
    {
        public static WebRequestConfig Request(ModId modId, string content, long commentId)
        {
            var request = new WebRequestConfig
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{(long)modId}/comments/{commentId}?", RequestMethodType = "PUT",
            };
            request.AddField("content", content);

            return request;
        }
    }
}
