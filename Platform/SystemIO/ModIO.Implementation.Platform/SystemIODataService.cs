#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || (MODIO_COMPILE_ALL && UNITY_EDITOR)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable 1998 // These async functions don't use await!

namespace ModIO.Implementation.Platform
{
    /// <summary>SystemIO implementation of the various data services.</summary>
    internal class SystemIODataService : IUserDataService, IPersistentDataService, ITempDataService
    {
#region Directories
#if UNITY_STANDALONE_WIN

        /// <summary>Root directory for persistent data.</summary>
        public static readonly string PersistentDataRootDirectory =
            Environment.GetEnvironmentVariable("public") + @"/mod.io";

        /// <summary>Root directory for User Specific data.</summary>
        public readonly static string UserRootDirectory =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"/mod.io";

#else
    /// <summary>Root directory for persistent data.</summary>
        public static readonly string PersistentDataRootDirectory =
        $"{Application.persistentDataPath}/mod.io";

        /// <summary>Root directory for User Specific data.</summary>
        public readonly static string UserRootDirectory =
            $"{Application.persistentDataPath}/UserData/mod.io";
#endif

        /// <summary>Root directory for Temporary data.</summary>
        public readonly static string TempRootDirectory = Application.temporaryCachePath;

        /// <summary>File path for the global settings file.</summary>
        public static readonly string GlobalSettingsFilePath =
            $"{UserRootDirectory}/globalsettings.json";

#endregion

#region Data
        /// <summary>Global Settings data structure.</summary>
        [Serializable]
        internal struct GlobalSettingsFile
        {
            public string RootLocalStoragePath;
        }

        /// <summary>Root directory for the data service.</summary>
        string rootDir;

        /// <summary>Root directory for the data service.</summary>
        public string RootDirectory
        {
            get {
                return rootDir;
            }
        }

#endregion // Data

#region Initialization

        /// <summary>Init as IUserDataService.</summary>
        async Task<Result> IUserDataService.InitializeAsync(string userProfileIdentifier,
                                                            long gameId, BuildSettings settings)
        {
            // TODO(@jackson): Make valid userProfileIdentifier

            rootDir = $"{UserRootDirectory}/{gameId.ToString("00000")}/{userProfileIdentifier}";
            Result result = SystemIOWrapper.CreateDirectory(rootDir);

            if(result.Succeeded())
            {
                Logger.Log(LogLevel.Verbose,
                           "Initialized SystemIODataService for User Data: " + rootDir);
            }
            else
            {
                Logger.Log(LogLevel.Error, "Failed to initialize SystemIODataService for User Data. "
                                               + $"\n.rootDirectory={rootDir} "
                                               + $"\n.result:[{result.code.ToString("00000")}]");

                result = ResultBuilder.Create(ResultCode.Init_UserDataFailedToInitialize);
            }

            return result;
        }


        /// <summary>Init as IPersistentDataService.</summary>
        async Task<Result> IPersistentDataService.InitializeAsync(long gameId,
                                                                  BuildSettings settings)
        {
            // Default root directory
            rootDir = $"{PersistentDataRootDirectory}/{gameId.ToString()}";
            // If standalone PC look for a user override root directory
#if UNITY_STANDALONE
            string desiredRootDir = rootDir;
            Result result;

            ResultAnd<byte[]> settingsRead =
                await SystemIOWrapper.ReadFileAsync(GlobalSettingsFilePath);
            result = settingsRead.result;

            GlobalSettingsFile gsData;

            var userDataPath = UserData.instance?.rootLocalStoragePath;
            if(!string.IsNullOrEmpty(userDataPath))
            {
                Logger.Log(
                    LogLevel.Verbose,
                    "RootLocalStoragePath loaded from existing user.json"
                    + $"\ndirectory={userDataPath}");
                desiredRootDir = userDataPath;
            }
            else if(result.Succeeded()
               && IOUtil.TryParseUTF8JSONData(settingsRead.value, out gsData, out result))
            {
                Logger.Log(
                    LogLevel.Verbose,
                    "RootLocalStoragePath loaded from existing globalsettings.json"
                    + $"\ndirectory={gsData.RootLocalStoragePath}");
                desiredRootDir = $"{gsData.RootLocalStoragePath}/{gameId.ToString()}";
            }
            else if(result.code == ResultCode.IO_FileDoesNotExist
                    || result.code == ResultCode.IO_DirectoryDoesNotExist)
            {
                gsData = new GlobalSettingsFile
                {
                    RootLocalStoragePath = PersistentDataRootDirectory,
                };

                byte[] fileData = IOUtil.GenerateUTF8JSONData(gsData);

                // ignore the result
                await SystemIOWrapper.WriteFileAsync(GlobalSettingsFilePath,
                    fileData);

                Logger.Log(LogLevel.Verbose,
                    "RootLocalStoragePath written to new globalsettings.json");
            }
            else // something else happened...
            {
                string message =
                    $"Unable to initialize the persistent data service. globalsettings.json could"
                    + $" not be parsed to load in the root data directory. FilePath: "
                    + $"{GlobalSettingsFilePath} - Result: [{result.code.ToString()}]";
                Logger.Log(LogLevel.Error, message);

                return result;
            }

            rootDir = desiredRootDir;
#endif

            Logger.Log(LogLevel.Verbose,
                "Initialized SystemIODataService for Persistent Data: " + rootDir);

            return ResultBuilder.Success;
        }

        /// <summary>Init as ITempDataService.</summary>
        async Task<Result> ITempDataService.InitializeAsync(long gameId, BuildSettings settings)
        {
            // TODO(@jackson): Test dir creation
            rootDir = $@"{TempRootDirectory}/{gameId.ToString()}";

            Logger.Log(LogLevel.Verbose,
                       "Initialized SystemIODataService for Temp Data: " + rootDir);

            return ResultBuilder.Success;
        }

#endregion // Initialization

#region Operations

        /// <summary>Opens a file stream for reading.</summary>
        public ModIOFileStream OpenReadStream(string filePath, out Result result)
        {
            return SystemIOWrapper.OpenReadStream(filePath, out result);
        }

        /// <summary>Opens a file stream for writing.</summary>
        public ModIOFileStream OpenWriteStream(string filePath, out Result result)
        {
            return SystemIOWrapper.OpenWriteStream(filePath, out result);
        }

        /// <summary>Reads an entire file asynchronously.</summary>
        public async Task<ResultAnd<byte[]>> ReadFileAsync(string filePath)
        {
            return await SystemIOWrapper.ReadFileAsync(filePath);
        }

        /// <summary>Writes an entire file asynchronously.</summary>
        public async Task<Result> WriteFileAsync(string filePath, byte[] data)
        {
            return await SystemIOWrapper.WriteFileAsync(filePath, data);
        }

        /// <summary> Deletes a file </summary>
        public Result DeleteFile(string filePath)
        {
            return SystemIOWrapper.DeleteFileGetResult(filePath);
        }

        /// <summary>Deletes a directory and its contents recursively.</summary>
        public Result DeleteDirectory(string directoryPath)
        {
            return SystemIOWrapper.DeleteDirectory(directoryPath);
        }

        public Result MoveDirectory(string directoryPath, string newDirectoryPath)
        {
            return SystemIOWrapper.MoveDirectory(directoryPath, newDirectoryPath);
        }

        public bool TryCreateParentDirectory(string path)
        {
            return SystemIOWrapper.TryCreateParentDirectory(path, out Result _);
        }

        public async Task<bool> IsThereEnoughDiskSpaceFor(long bytes)
        {
            // Not implemented for this platform
            return true;
        }

#endregion // Operations

#region Utility

        /// <summary>Determines whether a file exists.</summary>
        public bool FileExists(string filePath)
        {
            return SystemIOWrapper.FileExists(filePath, out Result r);
        }

        /// <summary>Gets the size and hash of a file.</summary>
        public async Task<ResultAnd<(long fileSize, string fileHash)>> GetFileSizeAndHash(
            string filePath)
        {
            return await SystemIOWrapper.GetFileSizeAndHash(filePath);
        }

        /// <summary>Determines whether a directory exists.</summary>
        public bool DirectoryExists(string directoryPath)
        {
            return SystemIOWrapper.DirectoryExists(directoryPath);
        }

        /// <summary>Lists all the files in the given directory recursively.</summary>
        public ResultAnd<List<string>> ListAllFiles(string directoryPath)
        {
            return SystemIOWrapper.ListAllFiles(directoryPath);
        }

#endregion // Utility
    }
}

#pragma warning restore 1998 // These async functions don't use await!

#endif // UNITY_STANDALONE_WIN || (MODIO_COMPILE_ALL && UNITY_EDITOR)
