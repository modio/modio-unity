
using JetBrains.Annotations;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class AuthenticateUser
    {
        public struct ResponseSchema
        {
            // (Note): use AccessTokenObject as the response schema
        }

        internal static RequestTemplate Template =
            new RequestTemplate { canCacheResponse = true, requireAuthToken = false,
                                  requestMethodType = WebRequestMethodType.POST,
                                  requestResponseType = WebRequestResponseType.Text };

        public static string InternalURL(string securityCode, out WWWForm form)
        {
            form = new WWWForm();
            form.AddField("api_key", Settings.server.gameKey);
            form.AddField("security_code", securityCode);

            return $"{Settings.server.serverURL}{@"/oauth/emailexchange"}?";
        }

        public static string ExternalURL(AuthenticationServiceProvider serviceProvider, string data,
                                         [CanBeNull] TermsHash? hash,
                                         [CanBeNull] string emailAddress, [CanBeNull] string nonce,
                                         [CanBeNull] OculusDevice? device,
                                         [CanBeNull] string userId, out WWWForm form)
        {
            string tokenFieldName = "appdata";

            string provider = "";

            switch(serviceProvider)
            {
                case AuthenticationServiceProvider.Steam:
                    provider = "steamauth";
                    tokenFieldName = "appdata";
                    break;
                case AuthenticationServiceProvider.Epic:
                    provider = "epicauth";
                    break;
                case AuthenticationServiceProvider.GOG:
                    provider = "galaxyauth";
                    tokenFieldName = "appdata";
                    break;
                case AuthenticationServiceProvider.Itchio:
                    provider = "itchioauth";
                    tokenFieldName = "itchio_token";
                    break;
                case AuthenticationServiceProvider.Oculus:
                    provider = "oculusauth";
                    tokenFieldName = "access_token";
                    break;
                case AuthenticationServiceProvider.Xbox:
                    provider = "xboxauth";
                    tokenFieldName = "xbox_token";
                    break;
                case AuthenticationServiceProvider.Switch:
                    provider = "switchauth";
                    tokenFieldName = "id_token";
                    break;
                case AuthenticationServiceProvider.Discord:
                    provider = "discordauth";
                    tokenFieldName = "discord_token";
                    break;
                case AuthenticationServiceProvider.Google:
                    provider = "googleauth";
                    tokenFieldName = "id_token";
                    break;
            }

            // Setup WWWForm
            form = new WWWForm();
            bool agreedTerms = false;
            agreedTerms = ResponseCache.termsHash.md5hash == hash?.md5hash;

            form.AddField(tokenFieldName, data);
            form.AddField("terms_agreed", agreedTerms.ToString());
            // Add email address if provided
            if(emailAddress != null)
            {
                form.AddField("email", emailAddress);
            }
            // Add Oculus fields
            if(serviceProvider == AuthenticationServiceProvider.Oculus)
            {
                form.AddField("nonce", nonce);
                form.AddField("user_id", userId);
                form.AddField("device", device == OculusDevice.Quest ? "quest" : "rift");
            }

            // Return URL endpoint
            return $"{Settings.server.serverURL}{@"/external/"}{provider}?";
        }
    }
}
