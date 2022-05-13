using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;

namespace ModIO.Implementation
{
    internal class CompressOperationMultiple : CompressOperationBase
    {
        public IEnumerable<byte[]> data;

        public CompressOperationMultiple(IEnumerable<byte[]> compressed, [CanBeNull] ProgressHandle progressHandle)
            : base(progressHandle)
        {
            this.data = compressed;
        }

        public override void Cancel()
        {
            cancel = true;
        }

        public override async Task<ResultAnd<MemoryStream>> Compress()
        {
            ResultAnd<MemoryStream> resultAnd = new ResultAnd<MemoryStream>();
            resultAnd.value = new MemoryStream();

            using(ZipOutputStream zipStream = new ZipOutputStream(resultAnd.value))
            {
                zipStream.SetLevel(3);

                foreach(var bytes in data)
                {
                    string entryName = Guid.NewGuid().ToString();

                    MemoryStream memoryStream = new MemoryStream();
                    memoryStream.Write(bytes, 0, bytes.Length);

                    await CompressStream(entryName, memoryStream, zipStream);

                    if(cancel || ModIOUnityImplementation.shuttingDown)
                    {
                        return Abort(resultAnd, $"Aborting while zipping images.");
                    }
                }

                zipStream.IsStreamOwner = false;
            }

            resultAnd.result = ResultBuilder.Success;
            return resultAnd;
        }
    }
}
