namespace ModIO.Implementation.API.Requests
{
    static class DeleteModMedia
    {
        public static WebRequestConfig Request(ModId modId, string[] filenames)
        {
            WebRequestConfig request = new WebRequestConfig
            {
                Url = $"{Settings.server.serverURL}/games/{Settings.server.gameId}/mods/{modId.id}/media?",
                RequestMethodType = "DELETE",
                ShouldRequestTimeout = false,
            };

            if (filenames == null)
                return request;

            foreach (string filename in filenames)
            {
                if(!string.IsNullOrWhiteSpace(filename))
                    request.AddField("images[]", filename);
            }

            return request;
        }
    }
}
