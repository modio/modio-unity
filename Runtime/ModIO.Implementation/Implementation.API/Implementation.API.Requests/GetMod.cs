namespace ModIO.Implementation.API.Requests
{

    internal static class GetMod
    {
        public static WebRequestConfig Request(long modId)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}?",
                RequestMethodType = "GET"
            };

            return request;
        }
    }
}
