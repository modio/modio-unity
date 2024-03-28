namespace ModIO.Implementation.API.Requests
{
    static class ReorderModMedia
    {
        public static WebRequestConfig Request(ModId modId, string[] orderedFilenames)
        {
            WebRequestConfig request = new WebRequestConfig
            {
                Url = $"{Settings.server.serverURL}/games/{Settings.server.gameId}/mods/{modId.id}/media/reorder?",
                RequestMethodType = "PUT",
                ShouldRequestTimeout = false,
            };

            if (orderedFilenames == null)
                return request;

            foreach (string filename in orderedFilenames)
            {
                if(!string.IsNullOrWhiteSpace(filename))
                    request.AddField("images[]", filename);
            }

            return request;
        }
    }
}
