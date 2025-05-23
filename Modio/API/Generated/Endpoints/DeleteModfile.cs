// <auto-generated />
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Modio.API.SchemaDefinitions;
using Modio.Errors;

namespace Modio.API
{
    public static partial class ModioAPI
    {
        public static partial class Files
        {
            /// <summary>Delete a modfile. Successful request will return `204 No Content`.<br/><br/>__NOTE:__ A modfile can never be removed if it is the current active release for the corresponding mod regardless of user permissions. Furthermore, this ability is only available if you are authenticated as the game administrator for this game _or_ are the original uploader of the modfile.</summary>
            internal static async Task<(Error error, JToken response204)> DeleteModfileAsJToken(
                long modId,
                long fileId
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/{modId}/files/{fileId}", ModioAPIRequestMethod.Delete, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson(request);
            }

            /// <summary>Delete a modfile. Successful request will return `204 No Content`.<br/><br/>__NOTE:__ A modfile can never be removed if it is the current active release for the corresponding mod regardless of user permissions. Furthermore, this ability is only available if you are authenticated as the game administrator for this game _or_ are the original uploader of the modfile.</summary>
            /// <param name="modId">Mod id</param>
            /// <param name="fileId">Modfile id</param>
            internal static async Task<(Error error, Response204? response204)> DeleteModfile(
                long modId,
                long fileId
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/{modId}/files/{fileId}", ModioAPIRequestMethod.Delete, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson<Response204>(request);
            }
        }
    }
}
