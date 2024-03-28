namespace ModIO.Implementation.API.Requests
{
    internal static class GetUserWalletBalance
    {
        public static WebRequestConfig Request()
        {
            var request = new WebRequestConfig()
            {
                //https://api.mod.io/v1/me/wallets
                Url = $"{Settings.server.serverURL}{@"/me/wallets?&game_id="}{Settings.server.gameId}",
                RequestMethodType = "GET",
                ShouldRequestTimeout = false,
            };

            return request;
        }
    }
}
