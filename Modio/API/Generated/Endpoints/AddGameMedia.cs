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
            /// <p>Upload new media to a game. The request `Content-Type` header __must__ be `multipart/form-data` to submit image files. Any request you make to this endpoint *should* contain a binary file for each of the fields you want to update below. Successful request will return [Message Object](#message-object).</p>
            /// <p>__NOTE:__ You can also add media to [your game's profile](https://mod.io/content) on the mod.io website. This is the recommended approach.</p>
            /// </summary>
            internal static async Task<(Error error, JToken updateGameMediaResponse)> AddGameMediaAsJToken(
                AddGameMediaRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/media", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.MultipartFormData);

                request.Options.AddBody(body);
                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson(request);
            }

            /// <summary>
            /// <p>Upload new media to a game. The request `Content-Type` header __must__ be `multipart/form-data` to submit image files. Any request you make to this endpoint *should* contain a binary file for each of the fields you want to update below. Successful request will return [Message Object](#message-object).</p>
            /// <p>__NOTE:__ You can also add media to [your game's profile](https://mod.io/content) on the mod.io website. This is the recommended approach.</p>
            /// </summary>
            /// <param name="body"></param>
            internal static async Task<(Error error, UpdateGameMediaResponse? updateGameMediaResponse)> AddGameMedia(
                AddGameMediaRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/media", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.MultipartFormData);

                request.Options.AddBody(body);
                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson<UpdateGameMediaResponse>(request);
            }
        }
    }
}
