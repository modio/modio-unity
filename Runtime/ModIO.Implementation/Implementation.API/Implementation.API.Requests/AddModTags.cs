namespace ModIO.Implementation.API.Requests
{

    internal static class AddModTags
    {
        public static WebRequestConfig Request(ModId modId, string[] tags)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{(long)modId}/tags?",
                RequestMethodType = "POST",
            };
            
            

            foreach(var tag in tags)
            {
                if(!string.IsNullOrWhiteSpace(tag))
                {
                    //is it possible that unity can take a bunch of tags and then add them to a list?
                    //while going through this, double check that the generated form complies with
                    //the server
                    request.AddField("tags[]", tag);
                }
            }
            return request;
        }
    }
}

/*

    ModIOUnity
    ModIOUnityImplementation
    ModIOCommunications
    WebRequestManager
    WebRequestRunner


*/