using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.Platform;
using ModIO.Util;
using Plugins.mod.io;

namespace ModIO.Implementation
{
    /// <summary>Static interface for reading and writing files.</summary>
    internal static class DataStorage
    {

        // TODO Revisit this later, as most of it's benefits are now met with the mutex. Maybe expand this to more generic tasks or for ModManagement
        internal static TaskQueueRunner taskRunner = new TaskQueueRunner(1, true, PlatformConfiguration.SynchronizedDataJobs);

        static Mutex FileWriteMutex = new Mutex();

        public static Mutex GetFileWriteMutex() => FileWriteMutex;

        static string TempImagesFolderPath => $@"{temp.RootDirectory}/images";

        static ModCollectionRegistry _regSavePending = null;
        static bool _regSaveRunning = false;

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

        public static string GetUploadFilePath(long modId)
        {
            string fileName = $"{modId}_" + DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss") + ".zip";
            const string uploadDirectoryName = "Upload";
            return Path.Combine(temp.RootDirectory, uploadDirectoryName, fileName);
        }

        /// <summary>Writes the user data to disk.</summary>
        public static async Task<Result> SaveUserDataAsync()
        {
            byte[] userDataJSON = IOUtil.GenerateUTF8JSONData(UserData.instance);

            return await user.WriteFileAsync($@"{user.RootDirectory}/{UserDataFilePath}", userDataJSON);
        }

        /// <summary>Writes the user data to disk.</summary>
        public static Result SaveUserData()
        {
            byte[] userDataJSON = IOUtil.GenerateUTF8JSONData(UserData.instance);

            return user.WriteFile($@"{user.RootDirectory}/{UserDataFilePath}", userDataJSON);
        }

        /// <summary>Reads the user data from disk.</summary>
        public static async Task<Result> LoadUserDataAsync()
        {
            var userDataReadTask =
                await user.ReadFileAsync($@"{user.RootDirectory}/{UserDataFilePath}");
            Result result = userDataReadTask.result;

            if(result.Succeeded()
               && IOUtil.TryParseUTF8JSONData(userDataReadTask.value, out UserData userData, out result))
            {
                UserData.instance = userData;
            }

            return result;
        }

        /// <summary>Reads the user data from disk.</summary>
        public static Result LoadUserData()
        {
            var userDataRead = user.ReadFile($@"{user.RootDirectory}/{UserDataFilePath}");

            if(userDataRead.result.Succeeded()
               && IOUtil.TryParseUTF8JSONData(userDataRead.value, out UserData userData, out userDataRead.result))
            {
                UserData.instance = userData;
            }
            else
            {
                UserData.instance = new UserData();
            }

            return ResultBuilder.Success;
        }

#endregion // User IO

#region Tags

        static string GenerateTagsPath() => $"{persistent.RootDirectory}/tags.json";

        public static async Task<Result> SaveTags(GameTagOptionObject[] tags)
        {
            string filePath = GenerateTagsPath();
            byte[] data = IOUtil.GenerateUTF8JSONData(tags);

            return await taskRunner.AddTask(TaskPriority.HIGH, 1, async () => await persistent.WriteFileAsync(filePath, data));
        }

        public static async Task<ResultAnd<GameTagOptionObject[]>> LoadTags()
        {
            string filePath = GenerateTagsPath();
            GameTagOptionObject[] tags = Array.Empty<GameTagOptionObject>();

            if (!persistent.FileExists(filePath))
                return ResultAnd.Create(ResultBuilder.Create(ResultCode.IO_FileDoesNotExist), tags);

            ResultAnd<byte[]> readResult = await persistent.ReadFileAsync(filePath);
            Result result = readResult.result;

            if (result.Succeeded())
                IOUtil.TryParseUTF8JSONData(readResult.value, out tags, out result);

            return ResultAnd.Create(result, tags);
        }

#endregion // Tags

#region Mod Browsing

        /// <summary>Generates the file path for an image URL.</summary>
        public static string GenerateImageCacheFilePath(string imageURL)
        {
            if(string.IsNullOrEmpty(imageURL))
            {
                Logger.Log(
                    LogLevel.Verbose,
                    ":INTERNAL: Attempted to generate a file path for a NULL/Empty image URL.");
                return null;
            }

            // NOTE(@jackson): According to the following Blog post, the expected number of URLs
            // required to generate a collision is 2^64 (2^37 is 137 billion) so MD5 seems totally
            // fine.
            // URL: https://blog.codinghorror.com/url-shortening-hashes-in-practice/

            string filename = IOUtil.GenerateMD5(imageURL);
            return Path.Combine(TempImagesFolderPath, $"{filename}.png");
        }

        public static Result DeleteStoredImage(string imageURL)
        {
            // - generate file path -
            string filePath = GenerateImageCacheFilePath(imageURL);
            if(filePath == null)
            {
                return ResultBuilder.Create(ResultCode.Internal_InvalidParameter);
            }

            Result result = temp.DeleteFile(filePath);

            return result;
        }

        public static void DeleteAllTempImages()
        {
            if (temp.DirectoryExists(TempImagesFolderPath))
            {
                temp.DeleteDirectory(TempImagesFolderPath);
            }
        }

        public static ResultAnd<ModIOFileStream> GetImageFileReadStream(string imageURL)
        {
            ModIOFileStream stream = temp.OpenReadStream(GenerateImageCacheFilePath(imageURL), out Result result);
            return ResultAnd.Create(result, stream);
        }

        public static ResultAnd<ModIOFileStream> GetImageFileWriteStream(string imageURL)
        {
            ModIOFileStream stream = temp.OpenWriteStream(GenerateImageCacheFilePath(imageURL), out Result result);
            return ResultAnd.Create(result, stream);
        }

        /// <summary>Attempts to retrieve an image from the temporary cache.</summary>
        public static async Task<ResultAnd<byte[]>> TryRetrieveImageBytes(string imageURL)
        {
            // - generate file path -
            string filePath = GenerateImageCacheFilePath(imageURL);
            if(filePath == null)
            {
                return ResultAnd.Create<byte[]>(ResultCode.Internal_InvalidParameter, null);
            }

            // - read -
            ResultAnd<byte[]> readResult = await taskRunner.AddTask(TaskPriority.HIGH, 1,
                async () => await temp.ReadFileAsync(filePath));

            if(!readResult.result.Succeeded())
            {
                return ResultAnd.Create<byte[]>(readResult.result, null);
            }

            // - success -
            return ResultAnd.Create(ResultCode.Success, readResult.value);
        }

        #endregion // Mod Browsing

#region Mod Management

        /// <summary>Generates the path for an installation directory.</summary>
        private static string GeneratePersistentInstallationDirectoryPath(long modId, long modfileId)
        {
            return $@"{persistent.RootDirectory}/mods/{modId}_{modfileId}";
        }

        private static string GenerateTemporaryInstallationDirectoryPath(long modId, long modfileId)
        {
            return $@"{temp.RootDirectory}/mods/temp/{modId}_{modfileId}";
        }

        /// <summary>Generates the path for a modfile archive.</summary>
        public static string GenerateModfileArchiveFilePath(long modId, long modfileId)
        {
            return $@"{temp.RootDirectory}/{modId}_{modfileId}.zip";
        }

        public static bool TryGetInstallationDirectory(long modId, long modfileId, out string directoryPath)
        {
            if (TempModSetManager.IsUnsubscribedTempMod(new ModId(modId)))
                return TryGetTempInstallationDirectory(modId, modfileId, out directoryPath);
            else
                return TryGetSubscribedInstallationDirectory(modId, modfileId, out directoryPath);
        }

        /// <summary>Tests if a mod installation directory exists.</summary>
        private static bool TryGetSubscribedInstallationDirectory(long modId, long modfileId, out string directoryPath)
        {
            directoryPath = GeneratePersistentInstallationDirectoryPath(modId, modfileId);
            return persistent.DirectoryExists(directoryPath);
        }

        private static bool TryGetTempInstallationDirectory(long modId, long modfileId, out string directoryPath)
        {
            directoryPath = GenerateTemporaryInstallationDirectoryPath(modId, modfileId);
            return persistent.DirectoryExists(directoryPath);
        }

        public static bool IsDirectoryValid(string directoryPath)
        {
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
            //path is a path to a file
            var filePath = GenerateModfileArchiveFilePath(modId, modfileId);
            if(temp.FileExists(filePath))
            {
                result = temp.DeleteFile(filePath);
                return result.Succeeded();
            }

            return true;
        }

        public static bool TryDeleteInstalledMod(long modId, long modfileId, out Result result)
        {
            if(TempModSetManager.IsUnsubscribedTempMod(new ModId(modId)))
                return TryDeleteInstalledTempMod(modId, modfileId, out result);
            else
                return TryDeleteInstalledPersistentMod(modId, modfileId, out result);
        }

        /// <summary>Deletes the installation directory matching the given mod-modfile
        /// pair.</summary>
        private static bool TryDeleteInstalledPersistentMod(long modId, long modfileId, out Result result)
        {
            string directory = GeneratePersistentInstallationDirectoryPath(modId, modfileId);

            result = persistent.DeleteDirectory(directory);

            return (result.Succeeded());
        }

        /// <summary>Deletes the temp installation directory matching the given mod-modfile
        /// pair.</summary>
        private static bool TryDeleteInstalledTempMod(long modId, long modfileId, out Result result)
        {
            string directory = GenerateTemporaryInstallationDirectoryPath(modId, modfileId);

            result = persistent.DeleteDirectory(directory);

            return (result.Succeeded());
        }

        /// <summary>Deletes the temp installation directory</summary>
        private static void DeleteInstalledTempModDirectory()
        {
            string directory = $@"{temp.RootDirectory}/mods/temp/";
            persistent.DeleteDirectory(directory);
        }

        public static bool MoveTempModToInstallDirectory(ModId modId, long fileId)
        {
            var tempModDir = GenerateTemporaryInstallationDirectoryPath(modId, fileId);
            if (persistent.DirectoryExists(tempModDir))
            {
                persistent.MoveDirectory(tempModDir, GeneratePersistentInstallationDirectoryPath(modId, fileId));
                return true;
            }
            return false;
        }

        /// <summary>Generates the path for the extraction directory.</summary>
        private static string GenerateExtractionDirectoryPath(long modId)
        {
            if(TempModSetManager.IsUnsubscribedTempMod(new ModId(modId)))
                return $@"{temp.RootDirectory}/installation";
            else
                return $@"{persistent.RootDirectory}/installation";
        }

        /// <summary>Deletes the extraction directory.</summary>
        public static void DeleteExtractionDirectory(long modId)
        {
            persistent.DeleteDirectory(GenerateExtractionDirectoryPath(modId));
        }

        /// <summary>Moves extraction directory to the given installation location.</summary>
        public static Result MakeInstallationFromExtractionDirectory(long modId, long modfileId)
        {
            string extractionDirPath = GenerateExtractionDirectoryPath(modId);
            TryGetInstallationDirectory(modId, modfileId, out string installDirPath);
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
        public static ResultAnd<string> GetModfileArchivePathIfValid(
            long modId, long modfileId, long expectedSize, string expectedHash)
        {
            string filePath = GenerateModfileArchiveFilePath(modId, modfileId);

            Result result = temp.GetFileSizeAndHash(filePath, out long fileSize, out string fileHash);

            if(!result.Succeeded())
            {
                return ResultAnd.Create<string>(result, null);
            }

            if(expectedSize != fileSize)
            {
                return ResultAnd.Create<string>(ResultCode.Internal_FileSizeMismatch, null);
            }

            if(expectedHash != fileHash)
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
            _regSavePending = registry;

            if (_regSaveRunning)
            {
                while (_regSaveRunning)
                {
                    await Task.Yield();
                }

                return ResultBuilder.Success;
            }

            _regSaveRunning = true;

            // TODO For some platform implementations, check build settings, and create a cooldown/batching feature if required
            string filePath = GenerateSystemRegistryFilePath();

            Result result = ResultBuilder.Success;
            while(_regSavePending != null && result.Succeeded())
            {
                byte[] data = IOUtil.GenerateUTF8JSONData(_regSavePending);
                _regSavePending = null;

                result = await persistent.WriteFileAsync(filePath, data);
            }

            _regSaveRunning = false;
            return result;
        }

        /// <summary>Reads the ModCollectionRegistry from disk.</summary>
        public static async Task<ResultAnd<ModCollectionRegistry>> LoadSystemRegistryAsync()
        {
            string filePath = GenerateSystemRegistryFilePath();

            if(!persistent.FileExists(filePath))
            {
                // Registry hasn't been created yet, returning new object
                return ResultAnd.Create(ResultBuilder.Success, new ModCollectionRegistry());
            }

            ResultAnd<byte[]> readResult = await persistent.ReadFileAsync(filePath);

            Result result = readResult.result;
            ModCollectionRegistry registry = null;

            if(result.Succeeded())
            {
                IOUtil.TryParseUTF8JSONData(readResult.value, out registry, out result);
            }

            return ResultAnd.Create(result, registry);
        }

        /// <summary>Reads the ModCollectionRegistry from disk.</summary>
        public static ResultAnd<ModCollectionRegistry> LoadSystemRegistry()
        {
            string filePath = GenerateSystemRegistryFilePath();

            if(!persistent.FileExists(filePath))
            {
                // Registry hasn't been created yet, returning new object
                return ResultAnd.Create(ResultBuilder.Success, new ModCollectionRegistry());
            }

            ResultAnd<byte[]> readResult = persistent.ReadFile(filePath);

            Result result = readResult.result;
            ModCollectionRegistry registry = null;

            if(result.Succeeded())
            {
                IOUtil.TryParseUTF8JSONData(readResult.value, out registry, out result);
            }

            return ResultAnd.Create(result, registry);
        }

        public static ModIOFileStream OpenArchiveReadStream(string filePath, out Result result)
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
        public static ModIOFileStream OpenArchiveEntryOutputStream(long modId, string relativePath,
                                                                   out Result result)
        {
            string absPath = $@"{GenerateExtractionDirectoryPath(modId)}/{relativePath}";
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

            List<string> fileList = null;
            uint resultCode = (dataService != null ? ResultCode.Success
                                                   : ResultCode.IO_DataServiceForPathNotFound);

            if(resultCode == ResultCode.Success)
            {
                ResultAnd<List<string>> filesResult = dataService.ListAllFiles(directoryPath);
                resultCode = filesResult.result.code;
                fileList = filesResult.value;
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
