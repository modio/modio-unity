using System.Collections.Generic;

namespace ModIO.Implementation.API.Requests
{
    internal static class DeleteDependency
    {
        public static WebRequestConfig Request(long modId, ICollection<ModId> mods)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}{@"/dependencies"}?",
                RequestMethodType = "Delete"
            };

            int count = 0;
            foreach(var mod in mods)
            {
                request.AddField($"dependencies[{count}]", (long)mod);
                count++;
                
                if(count > 4)
                {
                    // The API is capped at 5 dependencies per request
                    break;
                }
            }

            return request;
        }
    }
}
