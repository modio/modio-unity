using System;
using Modio.API.SchemaDefinitions;
using Modio.Extensions;

namespace Modio.Mods
{
    public struct ModfileDownloadReference
    {
        public readonly string BinaryUrl;
        public readonly DateTime ExpiresAfter;

        internal ModfileDownloadReference(string binaryUrl, DateTime expiresAfter)
        {
            BinaryUrl = binaryUrl;
            ExpiresAfter = expiresAfter;
        }

        internal ModfileDownloadReference(DownloadObject downloadObject)
        {
            BinaryUrl = downloadObject.BinaryUrl;
            ExpiresAfter = downloadObject.DateExpires.GetUtcDateTime();
        }
    }
}
