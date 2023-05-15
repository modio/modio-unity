namespace ModIO.Implementation.API.Requests
{
    internal static class DeleteModTags
    {

        public static WebRequestConfig Request(ModId modId, string[] tags)
        {

            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{(long)modId}/tags?",
                RequestMethodType = "DELETE",
            };

            
            foreach(var tag in tags)
            {
                if(!string.IsNullOrWhiteSpace(tag))
                {
                    request.AddField("tags[]", tag);
                }
            }
            return request;
        }
    }
}
