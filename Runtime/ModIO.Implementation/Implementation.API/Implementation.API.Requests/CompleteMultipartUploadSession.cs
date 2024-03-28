
namespace ModIO.Implementation.API.Requests
{
    static class CompleteMultipartUploadSession
    {
        public static WebRequestConfig Request(ModId modId, string uploadId)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId.id}{@"/files/multipart/complete?upload_id="}{uploadId}",
                RequestMethodType = "POST",
            };

            return request;
        }
    }
}
