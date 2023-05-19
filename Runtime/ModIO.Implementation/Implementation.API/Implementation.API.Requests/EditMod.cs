namespace ModIO.Implementation.API.Requests
{

    internal static class EditMod
    {
        public static WebRequestConfig RequestPOST(ModProfileDetails details)
            => InternalRequest(details, "POST");

        public static WebRequestConfig RequestPUT(ModProfileDetails details)
            => InternalRequest(details, "PUT");
        
        public static WebRequestConfig InternalRequest(ModProfileDetails details, string requestType)
        {
            long modId = details.modId != null ? details.modId.Value.id : 0;
            
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}?",
                RequestMethodType = requestType
            };
            
            request.AddField("visible", details.visible == false ? "0" : "1");
            request.AddField("name", details.name);
            request.AddField("summary", details.summary);
            request.AddField("description", details.description);
            request.AddField("name_id", details.name_id);
            request.AddField("homepage_url", details.homepage_url);
            request.AddField("stock", details.maxSubscribers.ToString());

            if(details.contentWarning != null)
                request.AddField("maturity_option", ((int)details.contentWarning).ToString());

            if(details.communityOptions != null)
                request.AddField("community_options", ((int)details.communityOptions).ToString());

            // TODO Currently the EditMod endpoint doesnt allow changing/adding tags
            // if(details.tags != null)
            // {
            //     for(int i = 0; i < details.tags.Count(); i++)
            //     {
            //         request.AddField($"tags[{i}]", details.tags[i]);
            //     }
            // }

            request.AddField("metadata_blob", details.metadata);

            if(details.logo != null)
                request.AddField("logo", "logo.png", details.GetLogo());

            return request;
        }
    }
}
