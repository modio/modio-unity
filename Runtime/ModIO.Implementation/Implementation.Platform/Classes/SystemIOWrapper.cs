using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using UnityEngine;

namespace ModIO.Implementation.Platform
{
    /// <summary>Wrapper for System.IO that handles exceptions and matches our interface.</summary>
    internal static class SystemIOWrapper
    {
#region Operations
        
        //There is no native method for checking if a file is open or in use, so we need to keep
        // track of the files we are opening and using manually. For now this is the simplest solve.
        static HashSet<string> currentlyOpenFiles = new HashSet<string>();

        /// <summary>Creates a FileStream for the purposes of reading.</summary>
        public static ModIOFileStream OpenReadStream(string filePath, out Result result)
        {
            ModIOFileStream fileStream = null;
            result = ResultBuilder.Unknown;

            if(IsPathValid(filePath, out result)
               && FileExists(filePath, out result))
            {
                FileStream internalStream = null;

                try
                {
                    internalStream = File.Open(filePath, FileMode.Open);
                    result = ResultBuilder.Success;
                }
                catch(Exception e)
                {
                    Logger.Log(LogLevel.Warning,
                               "Unhandled error when attempting to create read FileStream."
                                   + $"\n.path={filePath}" + $"\n.Exception:{e.Message}");

                    internalStream = null;
                    result = ResultBuilder.Create(ResultCode.IO_FileCouldNotBeRead);
                }

                fileStream = new FileStreamWrapper(internalStream);
            }

            Logger.Log(LogLevel.Verbose,
                       $"Create read FileStream: {filePath} - Result: [{result.code}]");

            return fileStream;
        }

        /// <summary>Creates a FileStream for the purposes of writing.</summary>
        public static ModIOFileStream OpenWriteStream(string filePath, out Result result)
        {
            ModIOFileStream fileStream = null;
            result = ResultBuilder.Unknown;

            if(IsPathValid(filePath, out result)
               && TryCreateParentDirectory(filePath, out result))
            {
                FileStream internalStream = null;

                try
                {
                    internalStream = File.Open(filePath, FileMode.Create);
                    result = ResultBuilder.Success;
                }
                catch(Exception e)
                {
                    Logger.Log(LogLevel.Warning,
                               "Unhandled error when attempting to create write FileStream."
                                   + $"\n.path={filePath}" + $"\n.Exception:{e.Message}");

                    internalStream = null;
                    result = ResultBuilder.Create(ResultCode.IO_FileCouldNotBeCreated);
                }

                fileStream = new FileStreamWrapper(internalStream);
            }

            Logger.Log(LogLevel.Verbose,
                       $"Create write FileStream: {filePath} - Result: [{result.code}]");

            return fileStream;
        }

        /// <summary>Reads a file.</summary>
        public static async Task<ResultAnd<byte[]>> ReadFileAsync(string filePath)
        {
            byte[] data = null;
            Result result;

            // If the file we wish to open is already open we yield the thread
            while(currentlyOpenFiles.Contains(filePath))
            {
                await Task.Yield();
            }
            
            // add this filepath to a table of all currently open files
            currentlyOpenFiles.Add(filePath);

            if(IsPathValid(filePath, out result)
               && DoesFileExist(filePath, out result))
            {
                try
                {
                    using(var sourceStream = File.Open(filePath, FileMode.Open))
                    {
                        data = new byte[sourceStream.Length];
                        await sourceStream.ReadAsync(data, 0, (int)sourceStream.Length);
                    }

                    result = ResultBuilder.Success;
                }
                catch(Exception e) // TODO(@jackson): Handle UnauthorizedAccessException
                {
                    Logger.Log(LogLevel.Warning, "Unhandled error when attempting to read the file."
                                                     + $"\n.path={filePath}"
                                                     + $"\n.Exception:{e.Message}");

                    result = ResultBuilder.Create(ResultCode.IO_FileCouldNotBeRead);
                }
            }

            Logger.Log(
                LogLevel.Verbose,
                $"Read file: {filePath} - Result: [{result.code}] - Data: {(data == null ? "NULL" : data.Length+"B")}");

            // now that we are done with this file, remove it from the table of open files
            currentlyOpenFiles.Remove(filePath);
            
            return ResultAnd.Create(result, data);
        }

        /// <summary>Writes a file.</summary>
        public static async Task<Result> WriteFileAsync(string filePath, byte[] data)
        {
            Result result = ResultBuilder.Success;
            
            if(data == null)
            {
                Logger.Log(LogLevel.Verbose,
                    "Was not given any data to write. Cancelling write operation."
                    + $"\n.path={filePath}");
                return result;
            }

            // NOTE @Jackson I'm not a huge fan of this but would like to hear ideas for a better solution
            // If the file we wish to open is already open we yield the thread
            while(currentlyOpenFiles.Contains(filePath))
            {
                await Task.Yield();
            }
            
            // add this filepath to a table of all currently open files
            currentlyOpenFiles.Add(filePath);

            if(IsPathValid(filePath, out result)
               && TryCreateParentDirectory(filePath, out result))
            {
                try
                {
                    using(var fileStream = File.Open(filePath, FileMode.Create))
                    {
                        fileStream.Seek(0, SeekOrigin.End);
                        await fileStream.WriteAsync(data, 0, data.Length);
                    }

                    result = ResultBuilder.Success;
                }
                catch(Exception e)
                {
                    Logger.Log(LogLevel.Error,
                               "Unhandled error when attempting to write the file."
                                   + $"\n.path={filePath}" + $"\n.Exception:{e.Message}");
                
                    result = ResultBuilder.Create(ResultCode.IO_FileCouldNotBeWritten);
                }
            }

            Logger.Log(LogLevel.Verbose, $"Write file: {filePath} - Result: [{result.code}]");
            
            // now that we are done with this file, remove it from the table of open files
            currentlyOpenFiles.Remove(filePath);
            
            return result;
        }

        /// <summary>Creates a directory.</summary>
        public static Result CreateDirectory(string directoryPath)
        {
            Result result;

            if(IsPathValid(directoryPath, out result)
               && !DirectoryExists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    result = ResultBuilder.Success;
                }
                catch(UnauthorizedAccessException e)
                {
                    // UnauthorizedAccessException
                    // The caller does not have the required permission.

                    Logger.Log(LogLevel.Verbose,
                               "UnauthorizedAccessException when attempting to create directory."
                                   + $"\n.path={directoryPath}" + $"\n.Exception:{e.Message}");

                    result = ResultBuilder.Create(ResultCode.IO_AccessDenied);
                }
                catch(Exception e)
                {
                    Logger.Log(LogLevel.Warning,
                               "Unhandled error when attempting to create the directory."
                                   + $"\n.path={directoryPath}" + $"\n.Exception:{e.Message}");

                    result = ResultBuilder.Create(ResultCode.IO_DirectoryCouldNotBeCreated);
                }
            }

            return result;
        }

        /// <summary>Deletes a directory and its contents recursively.</summary>
        public static Result DeleteDirectory(string path)
        {
            Result result;

            if(IsPathValid(path, out result))
            {
                try
                {
                    if(Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }

                    result = ResultBuilder.Success;
                }
                catch(IOException e)
                {
                    // IOException
                    // A file with the same name and location specified by path exists.
                    // -or-
                    // The directory specified by path is read-only, or recursive is false and path
                    // is not an empty directory.
                    // -or-
                    // The directory is the application's current working directory.
                    // -or-
                    // The directory contains a read-only file.
                    // -or-
                    // The directory is being used by another process.

                    Logger.Log(LogLevel.Verbose, "IOException when attempting to delete directory."
                                                     + $"\n.path={path}"
                                                     + $"\n.Exception:{e.Message}");

                    result = ResultBuilder.Create(ResultCode.IO_AccessDenied);
                }
                catch(UnauthorizedAccessException e)
                {
                    // UnauthorizedAccessException
                    // The caller does not have the required permission.

                    Logger.Log(LogLevel.Verbose,
                               "UnauthorizedAccessException when attempting to delete directory."
                                   + $"\n.path={path}" + $"\n.Exception:{e.Message}");

                    result = ResultBuilder.Create(ResultCode.IO_AccessDenied);
                }
                catch(Exception e)
                {
                    Logger.Log(LogLevel.Warning,
                               "Unhandled error when attempting to create the directory."
                                   + $"\n.path={path}" + $"\n.Exception:{e.Message}");

                    result = ResultBuilder.Create(ResultCode.IO_DirectoryCouldNotBeDeleted);
                }
            }

            return result;
        }

        public static Result MoveDirectory(string directoryPath, string newDirectoryPath)
        {
            Result result = default;
            
            try
            {
                Directory.Move(directoryPath, newDirectoryPath);
            }
            catch(IOException e)
            {
                // IOException
                // A file with the same name and location specified by path exists.
                // -or-
                // The directory specified by path is read-only, or recursive is false and path
                // is not an empty directory.
                // -or-
                // The directory is the application's current working directory.
                // -or-
                // The directory contains a read-only file.
                // -or-
                // The directory is being used by another process.

                Logger.Log(LogLevel.Verbose, "IOException when attempting to move directory."
                                             + $"\n.path={directoryPath}"
                                             + $"\n.Exception:{e.Message}");

                result = ResultBuilder.Create(ResultCode.IO_AccessDenied);
            }
            catch(UnauthorizedAccessException e)
            {
                // UnauthorizedAccessException
                // The caller does not have the required permission.

                Logger.Log(LogLevel.Verbose,
                    "UnauthorizedAccessException when attempting to move directory."
                    + $"\n.path={directoryPath}" + $"\n.Exception:{e.Message}");

                result = ResultBuilder.Create(ResultCode.IO_AccessDenied);
            }
            catch(Exception e)
            {
                Logger.Log(LogLevel.Warning,
                    "Unhandled error when attempting to move directory."
                    + $"\n.path={directoryPath}" + $"\n.Exception:{e.Message}");

                result = ResultBuilder.Create(ResultCode.IO_DirectoryCouldNotBeDeleted);
            }

            return result;
        }

#endregion // Operations

#region Utility

        /// <summary>Checks that a file path is valid.</summary>
        public static bool IsPathValid(string filePath, out Result result)
        {
            // TODO(@jackson)
            // --- FileInfo ---
            // PathTooLongException
            // The specified path, file name, or both exceed the system-defined maximum length.
            // NotSupportedException
            // fileName contains a colon (:) in the middle of the string.
            // --- FileStream ---
            // ArgumentException
            // .NET Framework and .NET Core versions older than 2.1: path is a zero-length string,
            // contains only white space, or contains one or more invalid characters. You can query
            // for invalid characters by using the GetInvalidPathChars() method.
            // ArgumentNullException
            // path is null.
            // DirectoryNotFoundException
            // The specified path is invalid, (for example, it is on an unmapped drive).
            // FileNotFoundException
            // The file specified in path was not found.
            // NotSupportedException
            // path is in an invalid format.

            if(string.IsNullOrEmpty(filePath))
            {
                result = ResultBuilder.Create(ResultCode.IO_FilePathInvalid);
                return false;
            }

            result = ResultBuilder.Success;
            return true;
        }

        /// <summary>Determines whether a file exists.</summary>
        public static bool FileExists(string path, out Result result)
        {
            if(File.Exists(path))
            {
                result = ResultBuilder.Success;
                return true;
            }
            result = ResultBuilder.Create(ResultCode.IO_FileDoesNotExist);
            return false;
        }

        /// <summary>Gets the size and hash of a file.</summary>
        public static async Task<ResultAnd<(long fileSize, string fileHash)>> GetFileSizeAndHash(
            string filePath)
        {
            long fileSize = -1;
            string fileHash = null;
            Result result;

            if(!IsPathValid(filePath, out result)
               || !DoesFileExist(filePath, out result))
            {
                return ResultAnd.Create(result, (fileSize, fileHash));
            }

            // get fileSize
            try
            {
                fileSize = (new FileInfo(filePath)).Length;
            }
            catch(UnauthorizedAccessException e)
            {
                // UnauthorizedAccessException
                // Access to fileName is denied.

                Logger.Log(LogLevel.Verbose,
                           "UnauthorizedAccessException when attempting to read file size."
                               + $"\n.path={filePath}" + $"\n.Exception:{e.Message}");

                return ResultAnd.Create(ResultCode.IO_AccessDenied, (fileSize, fileHash));
            }
            catch(Exception e)
            {
                Logger.Log(LogLevel.Warning, "Unhandled error when attempting to get file size."
                                                 + $"\n.path={filePath}"
                                                 + $"\n.Exception:{e.Message}");

                return ResultAnd.Create(ResultCode.Unknown, (fileSize, fileHash));
            }

            // get hash
            ResultAnd<string> hashResult;
            try
            {
                using(var stream = File.OpenRead(filePath))
                {
                    hashResult = await IOUtil.GenerateMD5Async(stream);
                    fileHash = hashResult.value;
                }
            }
            catch(UnauthorizedAccessException e)
            {
                // UnauthorizedAccessException
                // path specified a directory.
                // -or-
                // The caller does not have the required permission.

                Logger.Log(LogLevel.Verbose,
                           "UnauthorizedAccessException when attempting to generate MD5 Hash."
                               + $"\n.path={filePath}" + $"\n.Exception:{e.Message}");

                return ResultAnd.Create(ResultCode.IO_AccessDenied, (fileSize, fileHash));
            }
            catch(IOException e)
            {
                // IOException
                // An I/O error occurred while opening the file.

                Logger.Log(LogLevel.Verbose, "IOException when attempting to generate MD5 Hash."
                                                 + $"\n.path={filePath}"
                                                 + $"\n.Exception:{e.Message}");

                return ResultAnd.Create(ResultCode.IO_FileCouldNotBeRead, (fileSize, fileHash));
            }
            catch(Exception e)
            {
                Logger.Log(LogLevel.Warning, "Unhandled error when attempting to get file hash."
                                                 + $"\n.path={filePath}"
                                                 + $"\n.Exception:{e.Message}");
                return ResultAnd.Create(ResultCode.Unknown, (fileSize, fileHash));
            }

            // success!
            return ResultAnd.Create(ResultCode.Success, (fileSize, fileHash));
        }

        /// <summary>Checks for the existence of a directory.</summary>
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>Determines whether a file exists.</summary>
        public static bool DoesFileExist(string filePath, out Result result)
        {
            if(!File.Exists(filePath))
            {
                result = ResultBuilder.Create(ResultCode.IO_FileDoesNotExist);
                return false;
            }

            result = ResultBuilder.Success;
            return true;
        }

        /// <summary>Attempts to create a parent directory.</summary>
        public static bool TryCreateParentDirectory(string filePath, out Result result)
        {
            string dirToCreate = Path.GetDirectoryName(filePath);
            if(Directory.Exists(dirToCreate))
            {
                result = ResultBuilder.Success;
                return true;
            }
            try
            {
                Directory.CreateDirectory(dirToCreate);

                result = ResultBuilder.Success;
                return true;
            }
            catch(Exception exception)
            {
                Logger.Log(
                    LogLevel.Warning,
                    $"Unhandled directory creation exception was thrown.\n.dirToCreate={dirToCreate}\n.exception={exception.Message}");

                result = ResultBuilder.Create(ResultCode.IO_DirectoryCouldNotBeCreated);
                return false;
            }
        }

        /// <summary>Lists all the files in the given directory recursively.</summary>
        public static ResultAnd<List<string>> ListAllFiles(string directoryPath)
        {
            const string AllFilesFilter = "*";

            if(!Directory.Exists(directoryPath))
            {
                return ResultAnd.Create<List<string>>(ResultCode.IO_DirectoryDoesNotExist, null);
            }

            try
            {
                // TODO(@jackson): Protect from infinite loops
                // https://docs.microsoft.com/en-us/dotnet/api/system.io.searchoption?view=net-5.0#remarks
                List<string> fileList = new List<string>();

                foreach(string filePath in Directory.EnumerateFiles(directoryPath, AllFilesFilter,
                                                                    SearchOption.AllDirectories))
                {
                    fileList.Add(filePath);
                }

                return ResultAnd.Create(ResultCode.Success, fileList);
            }
            catch(PathTooLongException e)
            {
                // PathTooLongException
                // The specified path, file name, or combined exceed the system-defined maximum
                // length.

                Logger.Log(LogLevel.Error,
                           "PathTooLongException when attempting to list directory contents."
                               + $"\n.directoryPath={directoryPath}" + $"\n.Exception:{e.Message}");

                return ResultAnd.Create<List<string>>(ResultCode.IO_FilePathInvalid, null);
            }
            catch(SecurityException e)
            {
                // SecurityException
                // The caller does not have the required permission.

                Logger.Log(LogLevel.Error,
                           "SecurityException when attempting to list directory contents."
                               + $"\n.directoryPath={directoryPath}" + $"\n.Exception:{e.Message}");

                return ResultAnd.Create<List<string>>(ResultCode.IO_AccessDenied, null);
            }
            catch(UnauthorizedAccessException e)
            {
                // UnauthorizedAccessException
                // The caller does not have the required permission.

                Logger.Log(LogLevel.Error,
                           "UnauthorizedAccessException when attempting to list directory contents."
                               + $"\n.directoryPath={directoryPath}" + $"\n.Exception:{e.Message}");

                return ResultAnd.Create<List<string>>(ResultCode.IO_AccessDenied, null);
            }
            catch(Exception e)
            {
                // ArgumentException
                // .NET Framework and .NET Core versions older than 2.1: path is a zero-length
                // string, contains only white space, or contains invalid characters. You can query
                // for invalid characters by using the GetInvalidPathChars() method. -or-
                // searchPattern does not contain a valid pattern.

                // ArgumentNullException
                // path is null.
                // -or-
                // searchPattern is null.

                // ArgumentOutOfRangeException
                // searchOption is not a valid SearchOption value.

                // DirectoryNotFoundException
                // path is invalid, such as referring to an unmapped drive.

                // IOException
                // path is a file name.

                Logger.Log(LogLevel.Error,
                           "Unhandled Exception when attempting to list directory contents."
                               + $"\n.directoryPath={directoryPath}" + $"\n.Exception:{e.Message}");

                return ResultAnd.Create<List<string>>(ResultCode.IO_AccessDenied, null);
            }
        }

#endregion // Utility

#region Legacy

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public static bool DeleteFile(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            try
            {
                if(File.Exists(path))
                {
                    File.Delete(path);
                }

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = $"[mod.io] Failed to delete file.\nFile: {path}\n\n" +
                    $"Exception: {e}\n\n";
                // Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        /// <summary>Moves a file.</summary>
        public static bool MoveFile(string source, string destination)
        {
            if(string.IsNullOrEmpty(source))
            {
                Debug.Log("[mod.io] Failed to move file. source is NullOrEmpty.");
                return false;
            }

            if(string.IsNullOrEmpty(destination))
            {
                Debug.Log("[mod.io] Failed to move file. destination is NullOrEmpty.");
                return false;
            }

            if(!DeleteFile(destination))
            {
                return false;
            }

            try
            {
                File.Move(source, destination);

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = "Failed to move file." + "\nSource File: {source}"
                                                            + $"\nDestination: {destination}\n\n"
                                                            + $"Exception: {e}\n\n";
                // Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        /// <summary>Gets the size of a file.</summary>
        public static Int64 GetFileSize(string path)
        {
            Debug.Assert(!String.IsNullOrEmpty(path));

            if(!File.Exists(path))
            {
                return -1;
            }

            try
            {
                var fileInfo = new FileInfo(path);

                return fileInfo.Length;
            }
            catch(Exception e)
            {
                string warningInfo = $"[mod.io] Failed to get file size.\nFile: {path}\n\nException {e}\n\n";
                // Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return -1;
            }
        }

        /// <summary>Gets the files at a location.</summary>
        public static IList<string> GetFiles(string path, string nameFilter,
                                             bool recurseSubdirectories)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if(!Directory.Exists(path))
            {
                return null;
            }

            var searchOption = (recurseSubdirectories ? SearchOption.AllDirectories
                                                      : SearchOption.TopDirectoryOnly);

            if(nameFilter == null)
            {
                nameFilter = "*";
            }

            return Directory.GetFiles(path, nameFilter, searchOption);
        }

        // --- Directory Management ---
        /// <summary>Moves a directory.</summary>
        // public static bool MoveDirectory(string source, string destination)
        // {
        //     Debug.Assert(!string.IsNullOrEmpty(source));
        //     Debug.Assert(!string.IsNullOrEmpty(destination));
        //
        //     try
        //     {
        //         Directory.Move(source, destination);
        //
        //         return true;
        //     }
        //     catch(Exception e)
        //     {
        //         string warningInfo = "[mod.io] Failed to move directory." + "\nSource Directory: "
        //                               + $"{source}\nDestination: {destination}\n\n"
        //                               + $"Exception: {e}";
        //         // + Utility.GenerateExceptionDebugString(e));
        //         // Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));
        //
        //         return false;
        //     }
        // }

        /// <summary>Gets the sub-directories at a location.</summary>
        public static IList<string> GetDirectories(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if(!Directory.Exists(path))
            {
                return null;
            }

            try
            {
                return Directory.GetDirectories(path);
            }
            catch(Exception e)
            {
                string warningInfo =
                    $"[mod.io] Failed to get directories.\nDirectory: {path}\n\n"
                    + $"Exception: {e}\n\n";

                // Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return null;
            }
        }

#endregion // Legacy
    }
}
