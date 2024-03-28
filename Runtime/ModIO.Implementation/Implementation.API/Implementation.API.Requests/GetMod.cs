using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{

    internal static class GetMod
    {
        public static WebRequestConfig<ModObject> Request(long modId)
        {
            return new WebRequestConfig<ModObject>()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}?",
                RequestMethodType = "GET"
            };
        }
    }
}
