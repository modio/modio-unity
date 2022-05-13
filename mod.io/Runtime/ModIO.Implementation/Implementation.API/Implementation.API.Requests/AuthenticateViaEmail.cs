
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class AuthenticateViaEmail
    {
        public struct ResponseSchema
        {
            public long code;
            public string message;
        }

        internal static RequestTemplate Template =
            new RequestTemplate { canCacheResponse = true, requireAuthToken = false,
                                  requestMethodType = WebRequestMethodType.POST,
                                  requestResponseType = WebRequestResponseType.Text };

        public static string URL(string emailaddress, out WWWForm form)
        {
            form = new WWWForm();
            form.AddField("api_key", Settings.server.gameKey);
            form.AddField("email", emailaddress);

            return $"{Settings.server.serverURL}{@"/oauth/emailrequest"}?";
        }
    }
}
