using System.Collections.Generic;

namespace ModIO.Implementation.API.Requests
{
    internal static class AddModMonetizationTeam
    {
        public static WebRequestConfig Request(long modId, ICollection<ModMonetizationTeamDetails> team)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}{@"/monetization/team"}?",
                RequestMethodType = "POST"
            };

            int count = 0;
            foreach(var teamMember in team)
            {
                request.AddField($"users[{count}][id]", teamMember.userId);
                request.AddField($"users[{count}][split]", teamMember.split);
                count++;
            }

            return request;
        }
    }
}
