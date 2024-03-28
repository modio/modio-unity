
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    static class AddMultipartUploadParts
    {
        private const int MEBIBYTE_50 = 52428800;
        public static WebRequestConfig Request(ModId modId, string uploadId, string contentRange, string digest, byte[] rawBytes)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId.id}{@"/files/multipart?upload_id="}{uploadId}",
                RequestMethodType = "PUT",
            };

            request.AddHeader("Content-Range", contentRange);

            if(!string.IsNullOrEmpty(digest))
                request.AddHeader("Digest", digest);

            if(rawBytes.Length <= MEBIBYTE_50)
            {
                request.RawBinaryData = rawBytes;
            }
            else
            {
                Debug.Log("Multi-upload part must be less than or equal to 50 MiBs.");
            }

            return request;
        }
    }
}
