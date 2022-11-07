using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.Platform;
using UnityEngine;

namespace ModIO.Implementation
{
    /// <summary>Static interface for reading and writing files.</summary>
    internal static class DataStorage
    {
        // TODO Revisit this later, as most of it's benefits are now met with the mutex. Maybe expand this to more generic tasks or for ModManagement
        internal static TaskQueueRunner taskRunner = new TaskQueueRunner(1, true, PlatformConfiguration.SynchronizedDataJobs);
        
        static Mutex FileWriteMutex = new Mutex();
        
        public static Mutex GetFileWriteMutex() => FileWriteMutex;
        
#region Data Services

        /// <summary>Persistent data storage service.</summary>
        public static IPersistentDataService persistent;

        /// <summary>User data storage service.</summary>
        public static IUserDataService user;

        /// <summary>Temporary data storage service.</summary>
        public static ITempDataService temp;

#endregion // Data Services

#region User IO

        const string UserDataFilePath = "user.json";

        /// <summary>Writes the user data to disk.</summary>
        public static async Task<Result> SaveUserData()
        {
            byte[] userDataJSON = IOUtil.GenerateUTF8JSONData(UserData.instance);

            return await taskRunner.AddTask(TaskPriority.HIGH, 1, 
                async () => await user.WriteFileAsync($@"{user.RootDirectory}/{UserDataFilePath}", userDataJSON));
        }

        /// <summary>Reads the user data from disk.</summary>
        public static async Task<Result> LoadUserData()
        {            
            var userDataReadTask = await taskRunner.AddTask(TaskPriority.HIGH, 1,
                async () => await user.ReadFileAsync($@"{user.RootDirectory}/{UserDataFilePath}"));
            Result result = userDataReadTask.result;

            if(result.Succeeded()
               && IOUtil.TryParseUTF8JSONData(userDataReadTask.value, out UserData userData, out result))
            {
                UserData.instance = userData;
            }

            return result;
        }

#endregion // User IO

#region Mod Browsing

        /// <summary>Generates the file path for an image URL.</summary>
        public static string GenerateImageCacheFilePath(string imageURL)
        {
            if(string.IsNullOrEmpty(imageURL))
            {
                Logger.Log(
                    LogLevel.Verbose,
                    ":INTERNAL: Attempted to generated a file path for a NULL/Empty image URL.");
                return null;
            }

            // NOTE(@jackson): According to the following Blog post, the expected number of URLs
            // required to generate a collision is 2^64 (2^37 is 137 billion) so MD5 seems totally
            // fine.
            // URL: https://blog.codinghorror.com/url-shortening-hashes-in-practice/

            string filename = IOUtil.GenerateMD5(imageURL);
            return $@"{temp.RootDirectory}/images/{filename}.png";
        }

        /// <summary>Stores an image to the temporary cache.</summary>
        public static async Task<Result> StoreImage(DownloadReference imageURL, Texture2D texture)
        {
            // - generate file path -
            string filePath = GenerateImageCacheFilePath(imageURL.url);
            if(filePath == null)
            {
                return ResultBuilder.Create(ResultCode.Internal_InvalidParameter);
            }

            // - process image -
            byte[] pngData = IOUtil.GeneratePNGData(texture);
            if(pngData == null)
            {
                return ResultBuilder.Create(ResultCode.Internal_InvalidParameter);
            }
            
            // - store -            
            Result result = await taskRunner.AddTask(TaskPriority.HIGH, 1, 
                async () => await temp.WriteFileAsync(filePath, pngData));
            
            return result;
        }

        /// <summary>Attempts to retrieve an image from the temporary cache.</summary>
        public static async Task<ResultAnd<Texture2D>> TryRetrieveImage(DownloadReference imageURL)
        {
            // - generate file path -
            string filePath = GenerateImageCacheFilePath(imageURL.url);
            if(filePath == null)
            {
                return ResultAnd.Create<Texture2D>(ResultCode.Internal_InvalidParameter, null);
            }

            // - read -
            ResultAnd<byte[]> readResult = await taskRunner.AddTask(TaskPriority.HIGH, 1, 
                async () => await temp.ReadFileAsync(filePath));
            
            if(!readResult.result.Succeeded())
            {
                return ResultAnd.Create<Texture2D>(readResult.result, null);
            }

            // - parse -
            Result parseResult;
            Texture2D texture;

            if(!IOUtil.TryParseImageData(readResult.value, out texture, out parseResult))
            {
                return ResultAnd.Create<Texture2D>(parseResult, null);
            }

            // - success -
            return ResultAnd.Create(ResultCode.Success, texture);
        }

#endregion // Mod Browsing

#region Mod Management

        /// <summary>Generates the path for the extraction directory.</summary>
        public static string GenerateExtractionDirectoryPath()
        {
            return $@"{persistent.RootDirectory}/installation";
        }

        /// <summary>Generates the path for an installation directory.</summary>
        public static string GenerateInstallationDirectoryPath(long modId, long modfileId)
        {
            return $@"{persistent.RootDirectory}/mods/{modId}_{modfileId}";
        }

        // REVIEW @Jackson TODO Please implement (if this is how you'd use it)
        /// <summary>Generates the path for an installation directory.</summary>
        public static string GenerateModfileDetailsDirectoryPath(string directory)
        {
            Debug.Log("Not Implemented Yet");
            return directory;
        }

        /// <summary>Generates the path for a modfile archive.</summary>
        public static string GenerateModfileArchiveFilePath(long modId, long modfileId)
        {
            return $@"{temp.RootDirectory}/{modId}_{modfileId}.zip";
        }

        /// <summary>Tests if a mod installation directory exists.</summary>
        public static bool TryGetInstallationDirectory(long modId, long modfileId,
                                                       out string directoryPath)
        {
            directoryPath = GenerateInstallationDirectoryPath(modId, modfileId);
            return persistent.DirectoryExists(directoryPath);
        }

        // REVIEW @Jackson TODO Please implement
        /// <summary>Tests if a modfile details directory exists.</summary>
        public static bool TryGetModfileDetailsDirectory(string directoryPath,
                                                         out string properDirectory)
        {
            properDirectory = GenerateModfileDetailsDirectoryPath(directoryPath);
            return persistent.DirectoryExists(directoryPath);
        }

        /// <summary>Tests to see if a modfile archive exists.</summary>
        public static bool TryGetModfileArchive(long modId, long modfileId, out string filePath)
        {
            filePath = GenerateModfileArchiveFilePath(modId, modfileId);
            return temp.FileExists(filePath);
        }

        /// <summary>Attempts to delete a modfile archive.</summary>
        public static bool TryDeleteModfileArchive(long modId, long modfileId, out Result result)
        {
            result = ResultBuilder.Success;
            return true;
        }

        /// <summary>Deletes the installation directory matching the given mod-modfile
        /// pair.</summary>
        public static bool TryDeleteInstalledMod(long modId, long modfileId, out Result result)
        {
            string directory = GenerateInstallationDirectoryPath(modId, modfileId);

            result = persistent.DeleteDirectory(directory);

            return (result.Succeeded());
        }

        /// <summary>Deletes the extraction directory.</summary>
        public static void DeleteExtractionDirectory()
        {
            persistent.DeleteDirectory(GenerateExtractionDirectoryPath());
        }

        /// <summary>Moves extraction directory to the given installation location.</summary>
        public static Result MakeInstallationFromExtractionDirectory(long modId, long modfileId)
        {
            string extractionDirPath = GenerateExtractionDirectoryPath();
            string installDirPath = GenerateInstallationDirectoryPath(modId, modfileId);
            Result result;

            try
            {
                result = persistent.DeleteDirectory(installDirPath);
                
                if(result.Succeeded()
                   && persistent.TryCreateParentDirectory(installDirPath))
                {
                    result = persistent.MoveDirectory(extractionDirPath, installDirPath);

                    if(!result.Succeeded())
                    {
                        Logger.Log(LogLevel.Error,
                            "Failed to move the extracted files into the proper directory."
                            + $"\n.src={extractionDirPath}" + $"\n.dest={installDirPath}");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Verbose,
                            "Moved the extracted files into the proper directory."
                            + $"\n.src={extractionDirPath}" + $"\n.dest={installDirPath}");
                    }
                }
            }
            catch(Exception e)
            {
                Logger.Log(LogLevel.Warning,
                           "Unhandled error when attempting to rename the extraction directory."
                               + $"\n.src={extractionDirPath}" + $"\n.dest={installDirPath}"
                               + $"\n.Exception:{e.Message}");

                result = ResultBuilder.Create(ResultCode.IO_DirectoryCouldNotBeMoved);
            }

            return result;
        }

        /// <summary>Tests to see if a modfile archive exists and matches the given info.</summary>
        public static async Task<ResultAnd<string>> GetModfileArchivePathIfValid(
            long modId, long modfileId, long expectedSize, string expectedHash)
        {
            string filePath = GenerateModfileArchiveFilePath(modId, modfileId);

            ResultAnd<(long fileSize, string fileHash)> sizeAndHashResult =
                await taskRunner.AddTask(TaskPriority.HIGH, 1, 
                    async () => await temp.GetFileSizeAndHash(filePath));

            if(!sizeAndHashResult.result.Succeeded())
            {
                return ResultAnd.Create<string>(sizeAndHashResult.result, null);
            }

            if(expectedSize != sizeAndHashResult.value.fileSize)
            {
                return ResultAnd.Create<string>(ResultCode.Internal_FileSizeMismatch, null);
            }

            if(expectedHash != sizeAndHashResult.value.fileHash)
            {
                return ResultAnd.Create<string>(ResultCode.Internal_FileHashMismatch, null);
            }

            return ResultAnd.Create(ResultBuilder.Success, filePath);
        }

        /// <summary>Generates the system registry path.</summary>
        public static string GenerateSystemRegistryFilePath()
        {
            return $@"{persistent.RootDirectory}/state.json";
        }

        /// <summary>Writes the ModCollectionRegistry to disk.</summary>
        public static async Task<Result> SaveSystemRegistry(ModCollectionRegistry registry)
        {
            // TODO For some platform implementations, check build settings, and create a cooldown/batching feature if required
            string filePath = GenerateSystemRegistryFilePath();
            byte[] data = IOUtil.GenerateUTF8JSONData(registry);

            return await taskRunner.AddTask(TaskPriority.HIGH, 1, 
                async () => await persistent.WriteFileAsync(filePath, data));
        }

        /// <summary>Reads the ModCollectionRegistry from disk.</summary>
        public static async Task<ResultAnd<ModCollectionRegistry>> LoadSystemRegistry()
        {
            string filePath = GenerateSystemRegistryFilePath();

            if(!persistent.FileExists(filePath))
            {
                // Registry hasn't been created yet, returning new object
                return ResultAnd.Create(ResultBuilder.Success, new ModCollectionRegistry());
            }

            ResultAnd<byte[]> readResult = await taskRunner.AddTask(TaskPriority.HIGH, 1, 
                async () => await persistent.ReadFileAsync(filePath));

            Result result = readResult.result;
            ModCollectionRegistry registry = null;

            if(result.Succeeded())
            {
                IOUtil.TryParseUTF8JSONData(readResult.value, out registry, out result);
            }

            return ResultAnd.Create(result, registry);
        }

        public static async Task<Result> SaveGameTags(GameTagOptionObject[] gameTags)
        {
            await Task.Delay(1);

            // TODO @Jackson
            Debug.LogWarning("Not Implemented Yet");
            return ResultBuilder.Success;
        }
        
        public static ModIOFileStream OpenArchiveReadStream(string filePath,
                                                            out Result result)
        {
            return temp.OpenReadStream(filePath, out result);
        }

        /// <summary>Opens an archive read stream.</summary>
        public static ModIOFileStream OpenArchiveReadStream(long modId, long modfileId,
                                                            out Result result)
        {
            string filePath = GenerateModfileArchiveFilePath(modId, modfileId);
            return OpenArchiveReadStream(filePath, out result);
        }

        /// <summary>Opens an archive output stream.</summary>
        public static ModIOFileStream OpenArchiveEntryOutputStream(string relativePath,
                                                                   out Result result)
        {
            string absPath = $@"{GenerateExtractionDirectoryPath()}/{relativePath}";
            return persistent.OpenWriteStream(absPath, out result);
        }

        /// <summary>Creates a modfile download output stream.</summary>
        public static ModIOFileStream CreateArchiveDownloadStream(string absolutePath,
                                                                  out Result result)
        {
            return temp.OpenWriteStream(absolutePath, out result);
        }

        /// <summary>
        /// Recursively iterates through the directory and creates file streams for each file found.
        /// </summary>
        /// <remarks>
        /// This function creates read-only streams and cannot be used to create write streams.
        /// </remarks>
        public static IEnumerable<ResultAnd<ModIOFileStream>> IterateFilesInDirectory(
            string directoryPath)
        {
            IDataService dataService = persistent;

#if UNITY_EDITOR
            // // Note from Steve: I dont see what the point of this is?
            // EditorDataService pds = (EditorDataService)DataStorage.persistent;
            // EditorDataService tds = (EditorDataService)DataStorage.temp;
            // EditorDataService uds = (EditorDataService)DataStorage.user;
            //
            // if(pds.CanHandlePath(directoryPath))
            // {
            //     dataService = pds;
            // }
            // else if(tds.CanHandlePath(directoryPath))
            // {
            //     dataService = tds;
            // }
            // else if(uds.CanHandlePath(directoryPath))
            // {
            //     dataService = uds;
            // }
            dataService = persistent;
#elif UNITY_STANDALONE || UNITY_GAMECORE
            dataService = DataStorage.persistent;
#endif // UNITY_EDITOR

            List<string> fileList = null;
            uint resultCode = (dataService != null ? ResultCode.Success
                                                   : ResultCode.IO_DataServiceForPathNotFound);

            if(resultCode == ResultCode.Success)
            {
                ResultAnd<List<string>> filesResult = dataService.ListAllFiles(directoryPath);
                resultCode = filesResult.result.code;
                fileList = filesResult.value;
                // Logger.Log(LogLevel.Error, $"Failed list all files. Result:
                // [{filesResult.result.code};{filesResult.result.code_api}]");
            }

            if(resultCode == ResultCode.Success)
            {
                Result result;
                foreach(string filePath in fileList)
                {
                    ModIOFileStream stream = dataService.OpenReadStream(filePath, out result);
                    if(result.Succeeded())
                    {
                        yield return ResultAnd.Create(result, stream);
                    }
                    else
                    {
                        Logger.Log(
                            LogLevel.Error,
                            $"Failed open stream. Result: [{result.code};{result.code_api}]");
                        resultCode = result.code;
                        break;
                    }
                }
            }

            if(resultCode != ResultCode.Success)
            {
                yield return ResultAnd.Create<ModIOFileStream>(
                    ResultCode.IO_DataServiceForPathNotFound, null);
            }
        }


#endregion // Mod Management
    }
}
