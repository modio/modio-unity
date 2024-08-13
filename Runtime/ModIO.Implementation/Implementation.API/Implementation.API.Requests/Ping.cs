namespace ModIO.Implementation.API.Requests
{
    internal static class Ping
    {
        public static WebRequestConfig Request()
        {
            return new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}/ping?",

                RequestMethodType = "GET"
            };
        }
    }
}
