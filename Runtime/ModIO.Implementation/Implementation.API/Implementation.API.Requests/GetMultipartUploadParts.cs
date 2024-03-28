using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    static class GetMultipartUploadParts
    {
        [System.Serializable]
        public class ResponseSchema : PaginatedResponse<MultipartUploadPart> { }

        public static WebRequestConfig Request(long modId, string uploadId, SearchFilter filter)
        {
            return new WebRequestConfig()
            {
                Url = FilterUtil.AddPagination(filter, $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{modId}{$@"/files/multipart?upload_id={uploadId}"}{FilterUtil.ConvertToURL(filter)}"),
                RequestMethodType = "GET",
            };
        }
    }
}
