using JetBrains.Annotations;

namespace ModIO.Implementation.API.Requests
{

    internal static class AuthenticateUser
    {
        public static WebRequestConfig InternalRequest(string securityCode)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/oauth/emailexchange"}?",                
                RequestMethodType = "POST",
            };
            
            request.AddField("api_key", Settings.server.gameKey);
            request.AddField("security_code", securityCode);

            return request;
        }

        public static WebRequestConfig ExternalRequest(AuthenticationServiceProvider serviceProvider, string data,
                                         [CanBeNull] TermsHash? hash,
                                         [CanBeNull] string emailAddress,
                                         [CanBeNull] string nonce,
                                         [CanBeNull] OculusDevice? device,
                                         [CanBeNull] string userId,
                                         PlayStationEnvironment environment)
        {
            var tokenFieldName = serviceProvider.GetTokenFieldName();
            var provider = serviceProvider.GetProviderName();

            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/external/"}{provider}?",
                RequestMethodType = "POST",
            };

            var agreedTerms = ResponseCache.termsHash.md5hash == hash?.md5hash;
            request.AddField(tokenFieldName, data);
            request.AddField("terms_agreed", agreedTerms.ToString());
            request.AddField("email", emailAddress);

            // Add Oculus fields
            if(serviceProvider == AuthenticationServiceProvider.Oculus)
            {
                request.AddField("nonce", nonce);
                request.AddField("user_id", userId);
                request.AddField("device", device == OculusDevice.Quest ? "quest" : "rift");
            }
            if(serviceProvider == AuthenticationServiceProvider.PlayStation)
            {
                request.AddField("env", ((int)environment).ToString());
            }

            return request;
        }
    }
}
