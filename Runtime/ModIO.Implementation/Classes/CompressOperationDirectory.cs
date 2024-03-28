using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace ModIO.Implementation
{


    /// <summary>
    /// Acts as a wrapper to handle a zip extraction operation. Can be cached for cancelling,
    /// pausing, etc
    /// </summary>

    internal class CompressOperationDirectory : CompressOperationBase
    {
        //theres a card to fix this

        readonly string directory;

        public CompressOperationDirectory(string directory, ProgressHandle progressHandle = null)
            : base(progressHandle)
        {
            this.directory = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
        }

        public override async Task<Result> Compress(Stream stream)
        {
            Logger.Log(LogLevel.Verbose, $"COMPRESS STARTED [{directory}]");

            Result result = new Result();

            using(ZipOutputStream zipStream = new ZipOutputStream(stream))
            {
                zipStream.SetLevel(3);

                //loop this across the directory, and set up the filestream etc
                var directories = DataStorage.IterateFilesInDirectory(directory);

                foreach(var dir in directories)
                {
                    if(dir.result.Succeeded() && !cancel && !ModIOUnityImplementation.shuttingDown)
                    {
                        using(dir.value)
                        {
                            string entryName = Path.GetFullPath(dir.value.FilePath).Substring(directory.Length);
                            await CompressStream(entryName, dir.value, zipStream);
                        }
                    }
                    else
                    {
                        Logger.Log(LogLevel.Error,
                                   cancel ? "Cancelled compress operation"
                                          : $"Failed to compress files at directory: "
                                                + $"{directory}\nResult[{dir.result.code}])");

                        return Abort(result, directory);
                    }
                }

                if(cancel || ModIOUnityImplementation.shuttingDown)
                {
                    return Abort(result, directory);
                }

                result = ResultBuilder.Success;
                zipStream.IsStreamOwner = false;
            }

            Logger.Log(LogLevel.Verbose, $"COMPRESSED [{result.code}] {directory}");
            result = ResultBuilder.Success;

            return result;
        }
    }
}
