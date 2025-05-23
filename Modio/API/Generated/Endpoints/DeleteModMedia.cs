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
        public static partial class Media
        {
            /// <summary>
            /// <p>Delete images, sketchfab or youtube links from a mod profile. Successful request will return `204 No Content`.</p>
            /// <p>__NOTE:__ You can also delete media from [your mod's profile](https://mod.io/content) on the mod.io website. This is the easiest way.</p>
            /// </summary>
            internal static async Task<(Error error, JToken response204)> DeleteModMediaAsJToken(
                long modId,
                DeleteModMediaRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/{modId}/media", ModioAPIRequestMethod.Delete, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.AddBody(body);
                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson(request);
            }

            /// <summary>
            /// <p>Delete images, sketchfab or youtube links from a mod profile. Successful request will return `204 No Content`.</p>
            /// <p>__NOTE:__ You can also delete media from [your mod's profile](https://mod.io/content) on the mod.io website. This is the easiest way.</p>
            /// </summary>
            /// <param name="modId">Mod id</param>
            /// <param name="body"></param>
            internal static async Task<(Error error, Response204? response204)> DeleteModMedia(
                long modId,
                DeleteModMediaRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/{modId}/media", ModioAPIRequestMethod.Delete, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.AddBody(body);
                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson<Response204>(request);
            }
        }
    }
}
