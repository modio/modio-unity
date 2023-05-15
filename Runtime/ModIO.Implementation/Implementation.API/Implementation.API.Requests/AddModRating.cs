namespace ModIO.Implementation.API.Requests
{

    internal static class AddModRating
    {
        public static WebRequestConfig Request(ModId modId, ModRating rating)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId.id}{@"/ratings"}?",
                RequestMethodType = "POST",
            };

            
            request.AddField("rating", ((int)rating).ToString());

            return request;
        }
    }
}
