namespace ModIO.Implementation.API.Requests
{
    internal static class UserUnmute
    {        
        public static WebRequestConfig Request(long userId)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/users/"}{userId}{@"/mute"}?",
                RequestMethodType = "DELETE"
            };
            
            return request;
        }
    }
}
