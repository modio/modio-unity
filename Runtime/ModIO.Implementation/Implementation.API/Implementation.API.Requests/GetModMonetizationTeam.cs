using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetModMonetizationTeam
    {
        [System.Serializable]
        internal class ResponseSchema : PaginatedResponse<MonetizationTeamAccountsObject> { }

        public static WebRequestConfig<ResponseSchema> Request(long modId)
        {
            return new WebRequestConfig<ResponseSchema>()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}{@"/monetization/team"}?",
                RequestMethodType = "GET"
            };
        }
    }
}
