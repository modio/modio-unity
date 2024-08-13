using System.Collections.Generic;
using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    static class AddModKvpMetadata
    {
        public static WebRequestConfig Request(long modId, Dictionary<string, string> metadata)
        {
            var request = new WebRequestConfig()
            {
                //games/{game-id}/mods/{mod-id}/metadatakvp
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}/metadatakvp",
                RequestMethodType = "POST",
                ShouldRequestTimeout = false,
            };

            foreach (var kvp in metadata)
            {
                request.AddField("metadata[]", $"{kvp.Key}:{kvp.Value}");
            }

            return request;
        }
    }
}
