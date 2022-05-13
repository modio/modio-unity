using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;

namespace ModIO.Implementation
{


    /// <summary>
    /// Acts as a wrapper to handle a zip extraction operation. Can be cached for cancelling,
    /// pausing, etc
    /// </summary>

    internal class CompressOperationDirectory : CompressOperationBase
    {
        //theres a card to fix this
        
        private string directory;

        public CompressOperationDirectory(string directory, [CanBeNull] ProgressHandle progressHandle = null)
            : base(progressHandle)
        {
            this.directory = directory;
        }


        public override async Task<ResultAnd<MemoryStream>> Compress()
        {
            ResultAnd<MemoryStream> resultAnd = new ResultAnd<MemoryStream>();
            resultAnd.value = new MemoryStream();

            using(ZipOutputStream zipStream = new ZipOutputStream(resultAnd.value))
            {
                zipStream.SetLevel(3);                
                int folderOffset = directory.Length + (directory.EndsWith("\\") ? 0 : 1);

                //loop this across the directory, and set up the filestream etc
                var directories = DataStorage.IterateFilesInDirectory(directory);

                foreach(var dir in directories)
                {
                    if(dir.result.Succeeded() && !cancel && !ModIOUnityImplementation.shuttingDown)
                    {
                        using(dir.value)
                        {
                            string entryName = GetEntryName(folderOffset, dir);
                            await CompressStream(entryName, dir.value, zipStream);
                        }
                    }
                    else
                    {
                        Logger.Log(LogLevel.Error,
                                   cancel ? "Cancelled compress operation"
                                          : $"Failed to compress files at directory: "
                                                + $"{directory}\nResult[{dir.result.code}])");

                        return Abort(resultAnd, directory);
                    }
                }

                if(cancel || ModIOUnityImplementation.shuttingDown)
                {
                    return Abort(resultAnd, directory);
                }

                resultAnd.result = ResultBuilder.Success;
                zipStream.IsStreamOwner = false; 
            }

            Logger.Log(LogLevel.Verbose, $"COMPRESSED [{resultAnd.result.code}] {directory}");
            resultAnd.result = ResultBuilder.Success;

            return resultAnd;
        }


        private static string GetEntryName(int folderOffset, ResultAnd<ModIOFileStream> dir)
        {
            // Make the name in zip based on the folder
            // eg,
            // Library/Application
            // Support/DefaultCompany/Shooter/mods/BobsMod/items/entryName

            // should become:

            // BobsMod/items/entryName
            return dir.value.FilePath.Substring(folderOffset);
        }
    }
}
