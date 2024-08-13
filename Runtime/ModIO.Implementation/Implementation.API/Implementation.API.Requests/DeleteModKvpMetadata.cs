using System.Collections.Generic;
using System.Text;
using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    static class DeleteModKvpMetadata
    {
        public static WebRequestConfig Request(long modId, Dictionary<string, string> metadata)
        {
            var request = new WebRequestConfig()
            {
                //  games/{game-id}/mods/{mod-id}/metadatakvp
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}/metadatakvp",
                RequestMethodType = "DELETE",
                ShouldRequestTimeout = false,
            };

            //Every pair to delete requires a separate field with metadata[] as the key
            //(eg. metadata[]=pistol-dmg:800, metadata[]=gravity:9.8).
            //NOTE: If the string contains only the key and no colon ':',
            //all metadata with that key will be removed.
            foreach (var kvp in metadata)
            {
                request.AddField("metadata[]", $"{kvp.Key}:{kvp.Value}");
            }

            return request;
        }
    }
}
