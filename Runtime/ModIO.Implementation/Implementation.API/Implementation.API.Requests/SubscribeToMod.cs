namespace ModIO.Implementation.API.Requests
{
    internal static class SubscribeToMod
    {
        public static WebRequestConfig Request(long modId)
        {
            var request = new WebRequestConfig()
            {
                Url =  $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}{@"/subscribe"}?",
                RequestMethodType = "POST"
            };



            return request;
        }
    }
}
