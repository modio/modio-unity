#if UNITY_EDITOR || (MODIO_COMPILE_ALL && UNITY_EDITOR)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable 1998 // These async functions don't use await!

namespace ModIO.Implementation.Platform
{
    /// <summary>Editor implementation of the data services.</summary>
    internal class EditorDataService : IUserDataService, IPersistentDataService, ITempDataService
    {
        /// <summary>Root directory for persistent data.</summary>
        public static readonly string PersistentDataRootDirectory =
        $"{Application.persistentDataPath}/mod.io";

        /// <summary>Root directory for User Specific data.</summary>
        public readonly static string UserRootDirectory =
            $"{Application.persistentDataPath}/UserData/mod.io";

        /// <summary>Root directory for Temporary data.</summary>
        public readonly static string TempRootDirectory = Application.temporaryCachePath;

        #region Data

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
        Result IUserDataService.Initialize(string userProfileIdentifier,
            long gameId, BuildSettings settings)
        {
            rootDir =
                $"{UserRootDirectory}/{gameId.ToString("00000")}/users/{userProfileIdentifier}";

            Logger.Log(LogLevel.Verbose, "Initialized EditorUserDataService: " + rootDir);

            return ResultBuilder.Success;
        }

        /// <summary>Init as IPersistentDataService.</summary>
        Result IPersistentDataService.Initialize(long gameId,
            BuildSettings settings)
        {
            rootDir =
                $"{PersistentDataRootDirectory}/{gameId.ToString("00000")}/data";

            Logger.Log(LogLevel.Verbose, "Initialized EditorPersistentDataService: " + rootDir);

            return ResultBuilder.Success;
        }

        /// <summary>Init as ITempDataService.</summary>
        Result ITempDataService.Initialize(long gameId, BuildSettings settings)
        {
            rootDir =
                $"{TempRootDirectory}/{gameId.ToString("00000")}/temp";

            Logger.Log(LogLevel.Verbose, "Initialized EditorTempDataService: " + rootDir);

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

        /// <summary>Reads an entire file asynchronously.</summary>
        public ResultAnd<byte[]> ReadFile(string filePath)
        {
            return SystemIOWrapper.ReadFile(filePath);
        }

        /// <summary>Writes an entire file asynchronously.</summary>
        public async Task<Result> WriteFileAsync(string filePath, byte[] data)
        {
            return await SystemIOWrapper.WriteFileAsync(filePath, data);
        }

        /// <summary>Writes an entire file asynchronously.</summary>
        public Result WriteFile(string filePath, byte[] data)
        {
            return SystemIOWrapper.WriteFile(filePath, data);
        }

        /// <summary>Deletes a file.</summary>
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

        /// <summary>Lists all the files in the given directory recursively.</summary>
        public ResultAnd<List<string>> ListAllFiles(string directoryPath)
        {
            return SystemIOWrapper.ListAllFiles(directoryPath);
        }

        /// <summary>Gets the size and hash of a file.</summary>
        public Result GetFileSizeAndHash(
            string filePath, out long fileSize, out string fileHash)
        {
            return SystemIOWrapper.GetFileSizeAndHash(filePath, out fileSize, out fileHash);
        }

        /// <summary>Determines whether a directory exists.</summary>
        public bool DirectoryExists(string directoryPath)
        {
            return SystemIOWrapper.DirectoryExists(directoryPath);
        }

        // /// <summary>Determines whether a path can be handled by this data service.</summary>
        // public bool CanHandlePath(string path)
        // {
        //     // NOTE(@jackson): For EditorDataService, all services handle all paths
        //     return true;
        // }

#endregion // Utility
    }
}
#pragma warning restore 1998 // These async functions don't use await!

#endif // UNITY_EDITOR || (MODIO_COMPILE_ALL && UNITY_EDITOR)
