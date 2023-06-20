using System;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace ModIO.Implementation
{
    abstract internal class CompressOperationBase : IModIOZipOperation
    {
        private const bool sizeLimitReached = false;
        protected bool cancel;

        protected ProgressHandle progressHandle;
        protected Task<ResultAnd<MemoryStream>> _operation;

        protected CompressOperationBase(ProgressHandle progressHandle)
        {
            this.progressHandle = progressHandle;
        }

        public Task GetOperation() => _operation;

        public virtual void Cancel() { }

        public void Dispose()
        {
            _operation?.Dispose();
        }

        public virtual Task<ResultAnd<MemoryStream>> Compress()
        {
            throw new NotImplementedException();
        }

        protected async Task CompressStream(string entryName, Stream fileStream, ZipOutputStream zipStream)
        {
            ZipEntry newEntry = new ZipEntry(entryName);

            zipStream.PutNextEntry(newEntry);

            long max = fileStream.Length;
            byte[] data = new byte[4096];
            fileStream.Position = 0;
            while(fileStream.Position < fileStream.Length)
            {
                // TODO @Jackson ensure ReadAsync and WriteAsync are
                // implemented on all filestream wrappers
                int size = await fileStream.ReadAsync(data, 0, data.Length);
                if(size > 0)
                {
                    await zipStream.WriteAsync(data, 0, size);
                    if(progressHandle != null)
                    {
                        // This is only the progress for the current entry
                        progressHandle.Progress = zipStream.Position / (float)max;
                    }
                }
                else
                {
                    break;
                }
            }

            zipStream.CloseEntry();
        }


        protected ResultAnd<MemoryStream> Abort(ResultAnd<MemoryStream> resultAnd, string details)
        {
            Logger.Log(LogLevel.Verbose,
               $"FAILED COMPRESSION [{resultAnd.result.code}] {details}");

            resultAnd.result = sizeLimitReached
                                   ? ResultBuilder.Create(ResultCode.IO_FileSizeTooLarge)
                                   : ResultBuilder.Create(ResultCode.Internal_OperationCancelled);

            return resultAnd;
        }
    }
}
