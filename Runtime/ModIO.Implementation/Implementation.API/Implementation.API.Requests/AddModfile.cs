using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ModIO.Implementation.API.Requests
{
    static class AddModFile
    {
        public static async Task<WebRequestConfig> Request(ModfileDetails details, Stream stream)
        {
            var id = details?.modId?.id ?? new ModId(0);

            var request = new WebRequestConfig()
            {
                Url = Url(id),
                RequestMethodType = "POST",
                ShouldRequestTimeout = false,
                ForceIsUpload = true
            };

            if(stream != null && stream.Length > 0)
            {
                if (stream.CanSeek)
                    stream.Position = 0;
                var result = new byte[stream.Length];
                var pos = await stream.ReadAsync(result, 0, (int)stream.Length, new CancellationToken());
                request.AddField("filehash", IOUtil.GenerateMD5(result));
                request.AddField("filedata", $"{id}_modfile.zip", result);
            }

            if(!string.IsNullOrEmpty(details.version))
                request.AddField("version", details.version);

            if(!string.IsNullOrEmpty(details.changelog))
                request.AddField("changelog", details.changelog);

            if(!string.IsNullOrEmpty(details.metadata))
                request.AddField("metadata_blob", details.metadata);

            if(!string.IsNullOrEmpty(details.uploadId))
                request.AddField("upload_id", details.uploadId);

            request.AddField("active", details.active);

            if (details.platforms != null)
            {
                foreach (string p in details.platforms)
                    if (!string.IsNullOrWhiteSpace(p))
                        request.AddField("platforms[]", p);
            }

            return request;
        }

        public static string Url(long id)=>$"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{id}{@"/files"}?";

    }
}
