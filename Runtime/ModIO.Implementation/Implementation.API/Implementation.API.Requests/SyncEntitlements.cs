using System.Net;
using System.Threading.Tasks;
using ModIO.Implementation.API.Objects;
using ModIO.Util;

#if UNITY_GAMECORE
using Unity.GameCore;
#endif

namespace ModIO.Implementation.API.Requests
{
    static class SyncEntitlements
    {
        [System.Serializable]
        internal class ResponseSchema : EntitlementPaginatedResponse<EntitlementObject> { }

        public static WebRequestConfig SteamRequest()
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/me/iap/steam/sync"}?",
                RequestMethodType = "POST",
                ShouldRequestTimeout = false,
            };

            return request;
        }
#if UNITY_GAMECORE
        /// <param name="xboxToken">The Xbox Live token returned from calling
        /// GetTokenAndSignatureAsync("POST", "https://*.modapi.io")
        /// NOTE: Due to the encrypted app ticket containing special
        /// characters, you must URL encode the string before sending
        /// the request to ensure it is successfully sent to our
        /// servers otherwise you may encounter an 422 Unprocessable
        /// Entity response. For Example, cURL will do this for you by
        /// using the --data-urlencode option</param>
        public static WebRequestConfig XboxRequest(string token)
        {
            var request = new WebRequestConfig {
                RequestMethodType = "POST",
                ShouldRequestTimeout = false,
                Url = $"{Settings.server.serverURL}{@"/me/iap/xboxlive/sync"}?"
            };

            request.AddField("xbox_token", token);
            return request;
        }
#endif
#if UNITY_PS4 || UNITY_PS5
        /// <param name="authCode">The auth code returned form the PSN Api</param>
        /// <param name="environment">The PSN environment you are targeting. If
        /// omitted, the request will default to targeting the production environment.</param>
        /// <param name="serviceLabel">The service label where the entitlement for mod.io reside.
        /// If omitted the default value will be 0.</param>
        public static WebRequestConfig PsnRequest(string authCode, PlayStationEnvironment environment, int serviceLabel)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/me/iap/psn/sync"}?",
                RequestMethodType = "POST",
                ShouldRequestTimeout = false,
            };
            request.AddField("auth_code", authCode);
            request.AddField("env", (int)environment);
            request.AddField("service_label", serviceLabel);

            return request;
        }
#endif
    }
}
