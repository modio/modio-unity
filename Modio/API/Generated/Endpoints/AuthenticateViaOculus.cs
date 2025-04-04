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
            /// <summary>Request an access token on behalf of an Meta Quest user. To use this functionality you *must* add your games [AppId and secret](https://dashboard.oculus.com/) from the Meta Quest Dashboard, to the *Game Admin > Settings* page of your games profile on mod.io. A Successful request will return an [Access Token Object](#access-token-object).</summary>
            public static async Task<(Error error, JToken accessTokenObject)> AuthenticateViaOculusAsJToken(
                MetaQuestAuthenticationRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/external/oculusauth", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.AddBody(body);

                return await _apiInterface.GetJson(request);
            }

            /// <summary>Request an access token on behalf of an Meta Quest user. To use this functionality you *must* add your games [AppId and secret](https://dashboard.oculus.com/) from the Meta Quest Dashboard, to the *Game Admin > Settings* page of your games profile on mod.io. A Successful request will return an [Access Token Object](#access-token-object).</summary>
            /// <param name="body"></param>
            public static async Task<(Error error, AccessTokenObject? accessTokenObject)> AuthenticateViaOculus(
                MetaQuestAuthenticationRequest? body = null
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/external/oculusauth", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.AddBody(body);

                return await _apiInterface.GetJson<AccessTokenObject>(request);
            }
        }
    }
}
