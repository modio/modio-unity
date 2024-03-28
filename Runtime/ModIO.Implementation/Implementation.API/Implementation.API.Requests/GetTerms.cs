namespace ModIO.Implementation.API.Requests
{
    internal static class GetTerms
    {
        public static WebRequestConfig Request()
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/authenticate/terms"}?",
                RequestMethodType = "GET",
                DontUseAuthToken = true
            };

            return request;
        }
    }
}
