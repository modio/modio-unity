using System;
using System.IO;
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

                Result result;

                var modId = details.modId.Value.id;
                using(ModIOFileStream fs = DataStorage.temp.OpenWriteStream(DataStorage.GetUploadFilePath(modId), out result))
                {
                    result = await zipOperation.Compress(fs);
                    fs.Position = 0;

                    if(result.Succeeded())
                    {
                        var fileContent = new byte[fs.Length];
                        var pos = await fs.ReadAsync(fileContent, 0, (int)fs.Length);
                        request.AddField("images", "images.zip", fileContent);
                    }
                    else
                    {
                        return ResultAnd.Create<WebRequestConfig>(result, null);
                    }
                }
            }

            return ResultAnd.Create(ResultBuilder.Success, request);
        }
    }
}
