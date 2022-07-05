namespace ModIO.Implementation.API.Requests
{
    internal static class UnsubscribeFromMod
    {
        // (NOTE): mod.io does not have a response schema for this request type

        public static readonly RequestConfig Template =
            new RequestConfig { requireAuthToken = true, canCacheResponse = false,
                                  requestMethodType = WebRequestMethodType.DELETE };

        public static string URL(long modId)
        {
            return $"{Settings.server.serverURL}{@"/games/"}"
                   + $"{Settings.server.gameId}{@"/mods/"}{modId}{@"/subscribe"}?";
        }
    }
}
