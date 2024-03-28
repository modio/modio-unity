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
            request.AddField("stock", details.stock);

            if (details.maturityOptions != null)
                request.AddField("maturity_option", ((int)details.maturityOptions).ToString());

            if (details.communityOptions != null)
                request.AddField("community_options", ((int)details.communityOptions).ToString());

            if (details.price != null)
                request.AddField("price", ((int)details.price).ToString());
            if (details.monetizationOptions != null)
                request.AddField("monetization_options", ((int)details.monetizationOptions).ToString());

            if (details.tags != null)
            {
                if (details.tags.Length == 0)
                    request.AddField("tags[]", "");
                else
                    foreach (string tag in details.tags)
                        if (!string.IsNullOrWhiteSpace(tag))
                            request.AddField("tags[]", tag);
            }

            request.AddField("metadata_blob", details.metadata);

            if (details.logo != null)
                request.AddField("logo", "logo.png", details.GetLogo());

            return request;
        }
    }
}
