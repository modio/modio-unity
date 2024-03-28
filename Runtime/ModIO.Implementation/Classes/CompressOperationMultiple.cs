using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace ModIO.Implementation
{
    internal class CompressOperationMultiple : CompressOperationBase
    {
        public IEnumerable<byte[]> data;

        public CompressOperationMultiple(IEnumerable<byte[]> compressed, ProgressHandle progressHandle)
            : base(progressHandle)
        {
            this.data = compressed;
        }

        public override void Cancel()
        {
            cancel = true;
        }

        public override async Task<Result> Compress(Stream stream)
        {
            Result result = ResultBuilder.Unknown;

            int count = 0;

            using(ZipOutputStream zipStream = new ZipOutputStream(stream))
            {
                zipStream.SetLevel(3);

                foreach(var bytes in data)
                {
                    string entryName = $"image_{count}.png";
                    count++;

                    using(Stream memoryStream = new MemoryStream())
                    {
                        await memoryStream.WriteAsync(bytes, 0, bytes.Length);
                        await CompressStream(entryName, memoryStream, zipStream);
                    }

                    if(cancel || ModIOUnityImplementation.shuttingDown)
                    {
                        return Abort(result, $"Aborting while zipping images.");
                    }
                }

                zipStream.IsStreamOwner = false;
            }

            return ResultBuilder.Success;
        }
    }
}
