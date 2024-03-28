namespace ModIO.Implementation.API.Requests
{
    static class CreateMultipartUploadSession
    {
        public static WebRequestConfig Request(long modId, string filename, string nonce)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}{@"/files/multipart?"}",
                RequestMethodType = "POST",
            };
            request.AddField("filename", filename);

            if(nonce != null)
                request.AddField("nonce", nonce);

            return request;
        }
    }
}
