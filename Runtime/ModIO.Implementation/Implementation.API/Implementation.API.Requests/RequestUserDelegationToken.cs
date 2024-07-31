using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class RequestUserDelegationToken
    {
        public static WebRequestConfig<UserDelegationToken> Request()
        {
            return new WebRequestConfig<UserDelegationToken>()
            {
                //https://{your-game-id}.modapi.io/v1/me/s2s/oauth/token
                Url = $"{Settings.server.serverURL}{@"/me/s2s/oauth/token"}",
                RequestMethodType = "POST"
            };
        }
    }
}
