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
        public static partial class Authentication
        {
            /// <summary>
            /// <p>Request an access token on behalf of an itch.io user via the itch.io desktop app. Due to the desktop application allowing multiple users to be logged in at once, if more than one user is logged in then the user at the top of that list on the itch.io login dialog will be the authenticating user. A Successful request will return an [Access Token Object](#access-token-object).</p>
            /// <p>__HINT:__ If you want to overlay the mod.io site in-game on itch.io, we recommend you add `?portal=itchio` to the end of the URL you open which will prompt the user to login with itch.io. See [Web Overlay Authentication](#web-overlay-authentication) for details.</p>
            /// </summary>
            internal static async Task<(Error error, JToken accessTokenObject)> AuthenticateViaItchioAsJToken(
                ItchioAuthenticationRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/external/itchioauth", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.AddBody(body);

                return await _apiInterface.GetJson(request);
            }

            /// <summary>
            /// <p>Request an access token on behalf of an itch.io user via the itch.io desktop app. Due to the desktop application allowing multiple users to be logged in at once, if more than one user is logged in then the user at the top of that list on the itch.io login dialog will be the authenticating user. A Successful request will return an [Access Token Object](#access-token-object).</p>
            /// <p>__HINT:__ If you want to overlay the mod.io site in-game on itch.io, we recommend you add `?portal=itchio` to the end of the URL you open which will prompt the user to login with itch.io. See [Web Overlay Authentication](#web-overlay-authentication) for details.</p>
            /// </summary>
            /// <param name="body"></param>
            internal static async Task<(Error error, AccessTokenObject? accessTokenObject)> AuthenticateViaItchio(
                ItchioAuthenticationRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/external/itchioauth", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.AddBody(body);

                return await _apiInterface.GetJson<AccessTokenObject>(request);
            }
        }
    }
}
