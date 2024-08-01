using System.Collections.Generic;
using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{

    internal static class GetModKvpMetadata
    {
        public static WebRequestConfig<Dictionary<string, string>> Request(long modId)
        {
            return new WebRequestConfig<Dictionary<string, string>>()
            {
                // /games/{game-id}/mods/{mod-id}/metadatakvp
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}/metadatakvp",
                RequestMethodType = "GET"
            };
        }
    }
}
