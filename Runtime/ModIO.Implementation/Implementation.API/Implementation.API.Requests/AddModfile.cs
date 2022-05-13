namespace ModIO.Implementation.API.Requests
{
    static class AddModFile
    {
        public static WebRequestConfig Request(ModfileDetails details, byte[] filedata)
        {
            var id = details?.modId?.id ?? new ModId(0);

            var request = new WebRequestConfig()
            {
                Url = Url(id),
                RequestMethodType = "POST",
                ShouldRequestTimeout = false,
            };

            

            request.AddField("version", details.version);
            request.AddField("changelog", details.changelog);
            request.AddField("filehash", IOUtil.GenerateMD5(filedata));
            request.AddField("metadata_blob", details.metadata);
            
            request.AddField("filedata", $"{id}_modfile.zip", filedata);

            return request;
        }

        public static string Url(long id)=>$"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{id}{@"/files"}?";
        
    }
}
