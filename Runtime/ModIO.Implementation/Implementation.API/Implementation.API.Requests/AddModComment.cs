namespace ModIO.Implementation.API.Requests
{

    internal static class AddModComment
    {
        public static WebRequestConfig Request(ModId modId, CommentDetails commentDetails)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{(long)modId}/comments?",
                RequestMethodType = "POST",
            };
            request.AddField("content", commentDetails.content);
            request.AddField("reply_id", commentDetails.replyId);

            return request;
        }
    }
}
