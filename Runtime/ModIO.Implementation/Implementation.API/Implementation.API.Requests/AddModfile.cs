using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ModIO.Implementation.API.Requests
{
    static class AddModFile
    {
        public static async Task<WebRequestConfig> Request(ModfileDetails details, MemoryStream stream)
        {
            var id = details?.modId?.id ?? new ModId(0);

            var request = new WebRequestConfig()
            {
                Url = Url(id),
                RequestMethodType = "POST",
                ShouldRequestTimeout = false,
            };

            stream.Seek(0, SeekOrigin.Begin);
            var result = new byte[stream.Length];
            var pos = await stream.ReadAsync(result, 0, (int)stream.Length, new CancellationToken());

            request.AddField("version", details.version);
            request.AddField("changelog", details.changelog);
            request.AddField("filehash", IOUtil.GenerateMD5(result));
            request.AddField("metadata_blob", details.metadata);

            request.AddField("filedata", $"{id}_modfile.zip", result);

            return request;
        }

        public static string Url(long id)=>$"{Settings.server.serverURL}{@"/games/"}{Settings.server.gameId}{@"/mods/"}{id}{@"/files"}?";

    }
}
