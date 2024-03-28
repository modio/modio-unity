using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetMutedUsers
    {
        [System.Serializable]
        public class ResponseSchema : PaginatedResponse<UserObject> { }

        public static WebRequestConfig Request()
        {
            var request = new WebRequestConfig
            {
                Url = Url,
                RequestMethodType = "GET"
            };

            return request;
        }

        public static string Url => $"{Settings.server.serverURL}{@"/me/users/muted"}?";
    }
}
