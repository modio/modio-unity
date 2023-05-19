namespace ModIO.Implementation.API.Requests
{

    internal static class AuthenticateViaEmail
    {
        public static WebRequestConfig Request(string emailaddress)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/oauth/emailrequest"}?",
                RequestMethodType = "POST",
                DontUseAuthToken = true
            };
            
            request.AddField("email", emailaddress);

            return request;
        }
    }
}
