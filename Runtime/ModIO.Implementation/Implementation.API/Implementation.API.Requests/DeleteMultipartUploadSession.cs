
namespace ModIO.Implementation.API.Requests
{
    static class DeleteMultipartUploadSession
    {
        public static WebRequestConfig Request(ModId modId, string uploadId)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId.id}{@"/files/multipart?upload_id="}{uploadId}",
                RequestMethodType = "DELETE",
            };

            return request;
        }
    }
}
