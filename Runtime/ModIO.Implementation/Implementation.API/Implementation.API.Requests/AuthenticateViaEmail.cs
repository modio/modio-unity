namespace ModIO.Implementation.API.Requests
{

    internal static class AuthenticateViaEmail
    {
        public struct ResponseSchema
        {
            public long code;
            public string message;
        }


        public static WebRequestConfig Request(string emailaddress)
        {
            var request = new WebRequestConfig()
            {
                //"https://api-staging.moddemo.io/v1/oauth/emailrequest?"
                //"https://api.mod.io/v1/oauth/emailrequest?"
                Url = //$"https://api.mod.io/v1/oauth/emailrequest?",
                    $"{Settings.server.serverURL}{@"/oauth/emailrequest"}?",
                RequestMethodType = "POST",
            };
            
            request.AddField("api_key", "31672f8640babdcfe91a4a12d16e3423"); //Settings.server.gameKey);
            request.AddField("email", emailaddress);

            return request;
        }
    }
}
