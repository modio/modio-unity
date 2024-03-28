namespace ModIO.Implementation.API.Requests
{
    internal static class PurchaseMod
    {
        // Idempotent must be alphanumeric and cannot contain unique characters except for -.
        public static WebRequestConfig Request(ModId modId, int displayAmount, string idempotent)
        {
            var request = new WebRequestConfig()
            {
                //https://api.mod.io/v1/games/{Settings.server.gameId}/mods/{modId.id}/checkout
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId.id}{@"/checkout?"}",
                RequestMethodType = "POST",
                ShouldRequestTimeout = false
            };

            request.AddField("display_amount", displayAmount);
            request.AddField("idempotent_key", idempotent);

            return request;
        }
    }
}
