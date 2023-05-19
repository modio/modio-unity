using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    static class AddModMedia
    {
        public static async Task<ResultAnd<WebRequestConfig>> Request(ModProfileDetails details)
        {
            var request = new WebRequestConfig()
            {
                Url = $"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{details.modId?.id}/media?",
                RequestMethodType = "POST",
                ShouldRequestTimeout = false,
            };

            if(details.logo != null)
                request.AddField("logo", "logo.png", details.logo.EncodeToPNG());

            if(details.images != null)
            {
                var imageBytes = details.GetGalleryImages();
                CompressOperationMultiple zipOperation = new CompressOperationMultiple(imageBytes, null);

                ResultAnd<MemoryStream> resultAnd = await zipOperation.Compress();

                if(resultAnd.result.Succeeded())
                {
                    request.AddField("images", "images.zip", resultAnd.value.ToArray());
                }
                else
                {
                    return ResultAnd.Create<WebRequestConfig>(resultAnd.result, null);
                }
            }
            
            return ResultAnd.Create(ResultBuilder.Success, request);
        }
    }
}
