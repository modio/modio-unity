using System;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

namespace ModIO.Implementation
{
    /// <summary>
    /// Acts as a wrapper to handle a zip extraction operation. Can be cached for cancelling,
    /// pausing, etc
    /// </summary>
    internal class ExtractOperation : IModIOZipOperation
    {
        public bool cancel;
        public long modId;
        public long fileId;
        public ProgressHandle progressHandle;

        Task IModIOZipOperation.GetOperation() => null;

        public ExtractOperation(long modId, long fileId,  ProgressHandle progressHandle = null)
        {
            this.modId = modId;
            this.fileId = fileId;
            this.progressHandle = progressHandle;
        }

        public async Task<Result> Extract()
        {
            return await DataStorage.taskRunner.AddTask(TaskPriority.HIGH, 1,
                async () => await ExtractAll(), true);
        }

        // ---------[ Interface ]---------
        /// <summary>Extracts the contents of an archive.</summary>
        async Task<Result> ExtractAll()
        {
            Logger.Log(LogLevel.Verbose, $"EXTRACTING [{modId}_{fileId}]");
            
            // First we need to check that we have enough disk space to complete this operation
            Result result = await IsThereEnoughSpaceForExtracting();
            if(!result.Succeeded())
            {
                return result;
            }
                
            using(Stream fileStream = DataStorage.OpenArchiveReadStream(modId, fileId, out result))
            {
                if(result.Succeeded())
                {
                    try
                    {
                        long max = fileStream.Length;

                        using(ZipInputStream stream = new ZipInputStream(fileStream))
                        {
                            ZipEntry entry;


                            stream.IsStreamOwner = false;

                            while((entry = stream.GetNextEntry()) != null)
                            {
                                if(!string.IsNullOrEmpty(entry.Name))
                                {
                                    // TODO @Steve even if we create this directory we get an IO
                                    // error because of a lack of permission to access the directory?
                                    // But regardless we should be cleaning out files like this. Do we
                                    // have a list of things we can auto ignore/clean?
                                    if(entry.Name.Contains("__MACOSX"))
                                    {
                                        continue;
                                    }
                                    if(entry.IsDirectory)
                                    {
                                        continue;
                                    }

                                    using(Stream streamWriter =
                                        DataStorage.OpenArchiveEntryOutputStream(entry.Name,
                                            out result))
                                    {
                                        if(result.Succeeded())
                                        {
                                            int size;
                                            byte[] data = new byte[1048760]; // 1 MiB buffer size
                                            while(true)
                                            {
                                                // Hard and fast cleanup if the operation is cancelled
                                                if(cancel || ModIOUnityImplementation.shuttingDown)
                                                {
                                                    // See end of method
                                                    cancel = true;
                                                    break;
                                                }

                                                // These don't need to be async as it's already running
                                                // on another thread (consider testing this on larger
                                                // mods, eg 5 GiB size mods)

                                                size = await stream.ReadAsync(data, 0, data.Length);
                                                if(size > 0)
                                                {
                                                    await streamWriter.WriteAsync(data, 0, size);
                                                    if(progressHandle != null)
                                                    {
                                                        // This is only the progress for the current
                                                        // entry
                                                        progressHandle.Progress =
                                                            stream.Position / (float)max;
                                                    }
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            cancel = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Logger.Log(LogLevel.Error,
                            $"Unhandled exception extracting file. MODFILE [{modId}_{fileId}. Exception: {e.Message}");
                        cancel = true;
                    }
                }
            }

            //-----------------------------------------------------------------------------------//
            //                            MOVE FINISHED EXTRACTION
            //-----------------------------------------------------------------------------------//
            if (!cancel)
            {
                try
                {
                    result = DataStorage.MakeInstallationFromExtractionDirectory(modId, fileId);
                    if(!result.Succeeded())
                    {
                        cancel = true;
                    }
                }
                catch(Exception e)
                {
                    Logger.Log(LogLevel.Error,
                        $"Unhandled exception extracting file. MODFILE [{modId}_{fileId}. Exception: {e.Message}");
                    cancel = true;
                }
            }

            //--------------------------------------------------------------------------------------
            // FINISH and/or CLEANUP
            
            if(cancel)
            {
                return CancelAndCleanup(result);
            }

            Logger.Log(LogLevel.Verbose,
                       $"EXTRACTED RESULT [{result.code}] MODFILE [{modId}_{fileId}]");
            return result;
        }

        Result CancelAndCleanup(Result result)
        {
            Logger.Log(LogLevel.Verbose,
                $"FAILED EXTRACTION [{result.code}] MODFILE [{modId}_{fileId}]");

            // Delete any files we may have already extracted
            DataStorage.TryDeleteInstalledMod(modId, fileId, out result);

            if(result.code == ResultCode.Unknown || result.code == ResultCode.Success)
            {
                // If result wasn't assigned, we have been cancelled
                result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);
            }

            return result;
        }

        async Task<Result> IsThereEnoughSpaceForExtracting()
        {
            // Get the extracted size first
            using(Stream fileStream = DataStorage.OpenArchiveReadStream(modId, fileId, out Result result))
            {
                if(result.Succeeded())
                {
                    try
                    {
                        using(ZipInputStream stream = new ZipInputStream(fileStream))
                        {
                            long uncompressedSize = 0;
                            ZipEntry entry;

                            while((entry = stream.GetNextEntry()) != null)
                            {
                                if(entry.Size == -1)
                                {
                                    Logger.Log(LogLevel.Verbose, $"Size Unknown for file in zip ({entry.Name}).");
                                }
                                else
                                {
                                    uncompressedSize += entry.Size;
                                }
                            }

                            // We may need to check for both temp and persistent because we extract
                            // into the temp directory and then we move the file to persistent when
                            // we confirm the md5 is correct
                            if(!await DataStorage.persistent.IsThereEnoughDiskSpaceFor(uncompressedSize)
                               || !await DataStorage.temp.IsThereEnoughDiskSpaceFor(uncompressedSize))
                            {
                                return ResultBuilder.Create(ResultCode.IO_InsufficientStorage);
                            }

                            return ResultBuilder.Success;
                        }
                    }
                    catch(Exception e)
                    {
                        Logger.Log(LogLevel.Error,
                            $"Unhandled exception trying to read archive's extract size. MODFILE [{modId}_{fileId}. Exception: {e.Message}");
                        return ResultBuilder.Create(ResultCode.IO_FileCouldNotBeRead);
                    }
                }
                Logger.Log(LogLevel.Error,
                    $"Unable to read archive file. MODFILE [{modId}_{fileId}. Result: [{result.code}]{ResultCode.GetErrorCodeMeaning(result.code)}");
                return ResultBuilder.Create(ResultCode.IO_FileCouldNotBeRead);
            }
        }

        // Implemented from IModIOZipOperation interface
        void IModIOZipOperation.Cancel()
        {
            cancel = true;
        }

        public void Dispose()
        {

        }

    }
}
