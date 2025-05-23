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
            /// <summary>Log the user out by revoking their current access token. If this request successfully completes, you should remove any tokens/cookies/cached credentials linking to the now-revoked access token so the user is required to login again through your application. Successful request will return a [Message Object](#message-object).</summary>
            internal static async Task<(Error error, JToken webMessageObject)> LogoutAsJToken(

            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/oauth/logout", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson(request);
            }

            /// <summary>Log the user out by revoking their current access token. If this request successfully completes, you should remove any tokens/cookies/cached credentials linking to the now-revoked access token so the user is required to login again through your application. Successful request will return a [Message Object](#message-object).</summary>
            internal static async Task<(Error error, WebMessageObject? webMessageObject)> Logout(

            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/oauth/logout", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson<WebMessageObject>(request);
            }
        }
    }
}
