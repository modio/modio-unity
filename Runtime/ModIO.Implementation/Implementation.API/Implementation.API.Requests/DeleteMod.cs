namespace ModIO.Implementation.API.Requests
{

    internal static class DeleteMod
    {
        public static WebRequestConfig Request(ModId modId)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId.id.ToString()}?",
                RequestMethodType = "DELETE",
            };

            return request;
        }
    }
}
