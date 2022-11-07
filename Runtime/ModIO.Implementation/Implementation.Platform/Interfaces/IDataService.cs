using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for the platform data service.</summary>
    internal interface IDataService
    {
        // --- Data ---
        /// <summary>Root directory for the data service.</summary>
        string RootDirectory { get; }

        // --- I/O Operations ---
        /// <summary>Opens a file stream for reading.</summary>
        ModIOFileStream OpenReadStream(string filePath, out Result result);

        /// <summary>Opens a file stream for writing.</summary>
        ModIOFileStream OpenWriteStream(string filePath, out Result result);

        /// <summary>Reads an entire file asynchronously.</summary>
        Task<ResultAnd<byte[]>> ReadFileAsync(string filePath);

        /// <summary>Writes an entire file asynchronously.</summary>
        Task<Result> WriteFileAsync(string filePath, byte[] data);

        // Task<Result> DeleteFileAsync(string filePath);

        /// <summary>Deletes a directory and its contents recursively.</summary>
        Result DeleteDirectory(string directoryPath);

        /// <summary> Moves a directory to a new filepath (Can also be used to rename) </summary>
        Result MoveDirectory(string directoryPath, string newDirectoryPath);

        bool TryCreateParentDirectory(string directoryPath);

        // --- Utility ---
        /// <summary>Determines whether a file exists.</summary>
        bool FileExists(string filePath);

        /// <summary>Gets the size and hash of a file.</summary>
        Task<ResultAnd<(long fileSize, string fileHash)>> GetFileSizeAndHash(string filePath);

        /// <summary>Determines whether a directory exists.</summary>
        bool DirectoryExists(string directoryPath);

        /// <summary>Lists all the files in the given directory recursively.</summary>
        ResultAnd<List<string>> ListAllFiles(string directoryPath);

        /// <summary>Gets the remaining amount of space available to write for this data service </summary>
        bool IsThereEnoughDiskSpaceFor(long numberOfBytes);
    }
}
