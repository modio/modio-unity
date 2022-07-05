using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ModIO.Implementation.API.Requests
{
    internal static class AddModMedia
    {
        public class AddModMediaUrlResult
        {
            public string url;
            public WWWForm form;
        }

        // public struct ResponseSchema
        // {
        //     // (NOTE): mod.io returns a MessageObject as the schema.
        //     // This schema will only be used if the server schema changes or gets expanded on
        // }

        public static readonly RequestConfig Template =
            new RequestConfig { requireAuthToken = true,
                                canCacheResponse = false,
                                requestResponseType = WebRequestResponseType.Text,
                                requestMethodType = WebRequestMethodType.POST,
                                ignoreTimeout = true };

        public static async Task<ResultAnd<AddModMediaUrlResult>> URL(ModProfileDetails details)
        {
            string url = $"{Settings.server.serverURL}{@"/games/"}"
                   + $"{Settings.server.gameId}{@"/mods/"}{details.modId?.id}/media?";


            var form = new WWWForm();

            if(details.logo != null)
            {
                form.AddBinaryData("logo", details.logo.EncodeToPNG(), "logo.png", null);
            }

            if(details.images != null)
            {
                CompressOperationMultiple zipOperation = new CompressOperationMultiple(
                    details.images.Select(x => x.EncodeToPNG()),
                    null);

                ResultAnd<MemoryStream> resultAnd = await zipOperation.Compress();

                if(resultAnd.result.Succeeded())
                {
                    form.AddBinaryData("images", resultAnd.value.ToArray(), "images.zip");
                }
                else
                {
                    return ResultAnd.Create(ResultBuilder.Unknown, new AddModMediaUrlResult()
                    {
                        url = url,
                        form = null
                    });
                }                    
            }

            return ResultAnd.Create(ResultBuilder.Success, new AddModMediaUrlResult
            {
                url = url,
                form = form
            });

        }
    }
}
