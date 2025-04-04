// <auto-generated />
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Modio.API.SchemaDefinitions;
using Modio.Errors;

namespace Modio.API
{
    public static partial class ModioAPI
    {
        public static partial class FilesMultipartUploads
        {
            /// <summary>Terminate an active multipart upload session, a successful request will return `204 No Content`.</summary>
            internal static async Task<(Error error, JToken response204)> DeleteMultipartUploadSessionAsJToken(
                string uploadId,
                long modId
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/{modId}/files/multipart", ModioAPIRequestMethod.Delete);

                request.Options.AddQueryParameter("upload_id", uploadId);
                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson(request);
            }

            /// <summary>Terminate an active multipart upload session, a successful request will return `204 No Content`.</summary>
            /// <param name="modId">Mod id</param>
            internal static async Task<(Error error, Response204? response204)> DeleteMultipartUploadSession(
                string uploadId,
                long modId
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/{modId}/files/multipart", ModioAPIRequestMethod.Delete);

                request.Options.AddQueryParameter("upload_id", uploadId);
                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson<Response204>(request);
            }
        }
    }
}
