using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Modio.Users;
using Modio.Errors;
using Modio.Mods;
using Newtonsoft.Json;

namespace Modio.FileIO
{
    /// <summary>
    /// SystemIO Implementation of <see cref="IModioDataStorage"/>
    /// </summary>
    public class BaseDataStorage : IModioDataStorage
    {
        protected bool Initialized;
        protected bool IsShuttingDown;

        protected long GameId;
        protected string Root;
        protected string UserRoot;

        protected int OngoingTaskCount;
        protected CancellationTokenSource ShutdownTokenSource;
        protected CancellationToken ShutdownToken;

        public virtual Task<Error> Init()
        {
            SetupRootPaths();

            OngoingTaskCount = 0;
            ShutdownTokenSource = new CancellationTokenSource();
            ShutdownToken = ShutdownTokenSource.Token;

            IsShuttingDown = false;
            Initialized = true;

            MigrateLegacyModInstalls();
            
            return Task.FromResult(Error.None);
        }

        protected virtual void SetupRootPaths()
        {
            GameId = ModioServices.Resolve<ModioSettings>().GameId;
            Root = 
                $"{Path.Combine(ModioServices.Resolve<IModioRootPathProvider>().Path, "mod.io", GameId.ToString())}{Path.DirectorySeparatorChar}";
            UserRoot
                = $"{Path.Combine(ModioServices.Resolve<IModioRootPathProvider>().UserPath, "mod.io", GameId.ToString())}{Path.DirectorySeparatorChar}";
        }

        public virtual async Task Shutdown()
        {
            var shutdownTimer = new Stopwatch();
            shutdownTimer.Start();

            ModioLog.Verbose?.Log($"Shutting down {typeof(BaseDataStorage)}");

            // We only need to cancel download & install operations
            // Any data writing should continue to the end for plugin stability
            IsShuttingDown = true;
            ShutdownTokenSource?.Cancel();

            while (OngoingTaskCount > 0) 
                await Task.Yield();

            shutdownTimer.Stop();
            ModioLog.Verbose?.Log($"{typeof(BaseDataStorage)} took {shutdownTimer.Elapsed.Milliseconds}ms to shut down");
        }

        [ModioDebugMenu]
        public static void DebugDeleteAllGameData()
        {
            ModioClient.DataStorage.DeleteAllGameData();
            User.LogOut();
        }

        public Task<Error> DeleteAllGameData()
        {
            if (!Initialized) return Task.FromResult(new Error(ErrorCode.NOT_INITIALIZED));

            Error error = DeleteDirectoryAndContents(Root);
            if (error) return Task.FromResult(error);
            
            error = DeleteDirectoryAndContents(UserRoot);
            if (error) return Task.FromResult(error);

            return Task.FromResult(Error.None);
        }

#region Basic Classes

        protected virtual async Task<(Error error, T result)> ReadData<T>(string filePath)
        {
            (Error error, string json) = await ReadTextFile(filePath);

            if (error)
            {
                ModioLog.Message?.Log(
                    $"Error reading the {typeof(T).Name} file at path {filePath}: {error.GetMessage()}"
                );

                return (error, default(T));
            }

            try
            {
                var output = JsonConvert.DeserializeObject<T>(json);
                return (error, output);
            }
            catch (Exception exception)
            {
                return (new ErrorException(exception), default(T));
            }
        }

        protected virtual async Task<Error> WriteData<T>(T data, string filePath)
        {
            if (data == null) return new Error(ErrorCode.BAD_PARAMETER);

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            Error error = await WriteTextFile(filePath, json);

            if (error)
                ModioLog.Error?.Log($"Error writing the {typeof(T).Name} file at path {filePath}: {error.GetMessage()}");

            return error;
        }

        Task<Error> DeleteData(string filePath)
        {
            Error error = DeleteFile(filePath);

            if (error)
                ModioLog.Error?.Log($"Error deleting [{GameId}] game data: {error.GetMessage()}\nAt: {filePath}");

            return Task.FromResult(error);
        }

#endregion

#region Game Data

        public virtual Task<(Error error, GameData result)> ReadGameData()
            => ReadData<GameData>(GetGameDataFilePath());

        public virtual Task<Error> WriteGameData(GameData gameData)
            => WriteData(gameData, GetGameDataFilePath());

        public virtual Task<Error> DeleteGameData() => DeleteData(GetGameDataFilePath());

#endregion

#region Mod Index

        public virtual Task<(Error error, ModIndex index)> ReadIndexData()
            => ReadData<ModIndex>(GetIndexFilePath());

        public virtual Task<Error> WriteIndexData(ModIndex index) => WriteData(index, GetIndexFilePath());

        public virtual Task<Error> DeleteIndexData() => DeleteData(GetIndexFilePath());

#endregion

#region User Data

        public virtual Task<(Error error, UserSaveObject result)> ReadUserData(string localUserId)
            => ReadData<UserSaveObject>(GetUserDataFilePath(localUserId));

        public virtual Task<Error> WriteUserData(UserSaveObject userObject) => WriteData(
            userObject,
            GetUserDataFilePath(userObject.LocalUserId)
        );

        public virtual Task<Error> DeleteUserData(string localUserId) => DeleteData(GetUserDataFilePath(localUserId));

        public virtual async Task<(Error error, UserSaveObject[] results)> ReadAllSavedUserData()
        {
            Error error = Error.None;
            var output = new List<UserSaveObject>();

            if (!Directory.Exists(Root)) 
                return (new Error(ErrorCode.DIRECTORY_NOT_FOUND), Array.Empty<UserSaveObject>());

            try
            {
                foreach (string localUserId in Directory.GetFiles(Root)
                                                        .Where(fileName => fileName.Contains("_user_data"))
                                                        .Select(Path.GetFileName)
                                                        .Select(fileName => fileName.Split('_')[0]))
                {
                    (Error error, UserSaveObject result) currentUserData = await ReadUserData(localUserId);

                    if (currentUserData.error) continue;

                    output.Add(currentUserData.result);
                }

                return (error, output.ToArray());
            }
            catch (Exception exception)
            {
                ModioLog.Error?.Log($"Exception reading all User Data files : {exception}");

                return (new ErrorException(exception), Array.Empty<UserSaveObject>());
            }
        }

#endregion

#region Modfile

        public virtual async Task<Error> DownloadModFileFromStream(
            long modId,
            long modfileId,
            Stream downloadStream,
            string md5Hash,
            CancellationToken token
        ) {
            string filePath = GetModfilePath(modId, modfileId);
            Error error = CreateDirectory(filePath);

            if (error)
            {
                ModioLog.Error?.Log($"Error attempting download Modfile: {error.GetMessage()}\nAt:{filePath}");

                downloadStream?.Dispose();
                return error;
            }

            var buffer = new byte[1024 * 1024]; // 1MB

            OngoingTaskCount++;

            try
            {
                // We create a combined token to listen for either shutdown or cancel
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ShutdownToken);
                token = combinedCts.Token;

                using var md5 = MD5.Create();

                await using (Stream writeStream = File.Open(filePath, FileMode.Create))
                {
                    int bytesRead;

                    while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        if (token.IsCancellationRequested)
                        {
                            ModioLog.Verbose?.Log("Cancelling");
                            error = new Error(IsShuttingDown ? ErrorCode.SHUTTING_DOWN : ErrorCode.OPERATION_CANCELLED);
                            break;
                        }

                        md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                        await writeStream.WriteAsync(buffer, 0, bytesRead, token);
                    }
                }

                downloadStream.Dispose();

                md5.TransformFinalBlock(buffer, 0, 0);
                string actualMd5Hash = BitConverter.ToString(md5.Hash).Replace("-", "").ToLowerInvariant();

                if (!string.Equals(md5Hash, actualMd5Hash))
                {
                    ModioLog.Error?.Log($"Validation failed for Modfile: At {filePath}");
                    error = new ModValidationError(ModValidationErrorCode.MD5DOES_NOT_MATCH);
                    Error deleteFileError = await DeleteModfile(modId, modfileId);

                    if (deleteFileError) error = deleteFileError;

                    return error;
                }
            }
            catch (TaskCanceledException exception)
            {
                ModioLog.Verbose?.Log($"Cancelled downloading Modfile: {exception}\nAt:{filePath}");

                error = new Error(IsShuttingDown ? ErrorCode.SHUTTING_DOWN : ErrorCode.OPERATION_CANCELLED);
                Error deleteFileError = DeleteFile(filePath);

                if (deleteFileError) error = deleteFileError;

                return error;
            }
            catch (Exception exception)
            {
                ModioLog.Error?.Log($"Exception attempting to download Modfile: {exception}\nAt:{filePath}");

                error = new ErrorException(exception);
                Error deleteFileError = DeleteFile(filePath);

                if (deleteFileError) error = deleteFileError;

                return error;
            }
            finally
            {
                OngoingTaskCount--;
            }

            return error;
        }

        /// <summary>
        /// Calculate a MD5 Hash
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static async Task<byte[]> CalculateMd5Hash(string filePath, byte[] buffer)
        {
            using MD5 md5 = MD5.Create();
            int bytesRead;

            await using Stream stream = File.OpenRead(filePath);

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            md5.TransformFinalBlock(buffer, 0, 0);
            return md5.Hash;
        }

        public virtual Task<Error> DeleteModfile(long modId, long modfileId)
        {
            string filePath = GetModfilePath(modId, modfileId);

            Error error = DeleteFile(filePath);

            if (error) ModioLog.Error?.Log($"Error deleting Modfile {modId}: {error.GetMessage()}\nAt: {filePath}");

            return Task.FromResult(error);
        }

        public virtual Task<(Error error, List<(long modId, long modfileId)> results)> ScanForModfiles()
        {
            // If directory doesn't exist we don't bother scanning as it's yet to be created
            if (!Directory.Exists(Path.Combine(Root, "Modfiles"))) 
                return Task.FromResult((Error.None, new List<(long, long)>()));

            try
            {
                string[] allFiles = Directory.GetFiles(Path.Combine(Root, "Modfiles"));

                var validPaths = new List<(long modId, long modfileId)>();

                foreach (string path in allFiles)
                {
                    if (!path.Contains("_modfile")) continue;

                    string fileName = Path.GetFileName(path);
                    string[] nameComponents = fileName.Split('_');

                    if (nameComponents.Length == 2 &&
                        long.TryParse(nameComponents[0], out long modId) &&
                        long.TryParse(nameComponents[1], out long modfileId))
                        validPaths.Add((modId, modfileId));
                    else
                        ModioLog.Message?.Log($"Invalid Modfile name: [{fileName}], skipping");
                }

                return Task.FromResult((Error.None, validPaths));
            }
            catch (Exception exception)
            {
                ModioLog.Error?.Log($"Exception scanning Modfiles: {exception}");

                return Task.FromResult(((Error)new ErrorException(exception), new List<(long, long)>()));
            }
        }

#endregion

#region Installation

        public virtual async Task<Error> InstallMod(Mod mod, long modfileId, CancellationToken token)
        {
            string modFilePath = GetModfilePath(mod.Id, modfileId);

            Error error = Error.None;

            if (!DoesFileExist(modFilePath)) error = new Error(ErrorCode.FILE_NOT_FOUND);

            if (!await IsThereEnoughSpaceForExtracting(modFilePath)) error = new Error(ErrorCode.INSUFFICIENT_SPACE);

            if (error)
            {
                ModioLog.Error?.Log($"Unable to install mod: {error.GetMessage()}\nModfile Path:{modFilePath}");
                return error;
            }

            Stream fileStream = File.Open(modFilePath, FileMode.Open);

            error = await InstallModFromStream(mod, modfileId, fileStream, null, token);

            if (error.Code is not ErrorCode.OPERATION_CANCELLED
                              and not ErrorCode.BAD_PARAMETER
                              and not ErrorCode.SHUTTING_DOWN)
                await DeleteModfile(mod.Id, modfileId);

            return error;
        }

        public virtual async Task<Error> InstallModFromStream(
            Mod mod,
            long modfileId,
            Stream stream,
            string md5Hash,
            CancellationToken token
        )
        {
            Error error = Error.None;

            string temporaryDirectoryPath = GetTemporaryInstallPath(mod.Id, modfileId);
            string installDirectoryPath = GetInstallPath(mod.Id, modfileId);
            Error directoryError = CreateDirectory(temporaryDirectoryPath);
            if (directoryError) error = directoryError;
            
            ModioLog.Message?.Log($"Installing Modfile {mod} to {installDirectoryPath}");

            if (error)
            {
                ModioLog.Error?.Log(
                    $"Unable to install mod: {error.GetMessage()}\nInstall Path:{installDirectoryPath}\nTemp Path:{temporaryDirectoryPath}\n"
                );

                stream?.Dispose();

                return error;
            }

            OngoingTaskCount++;

            try
            {
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ShutdownToken);
                token = combinedCts.Token;

                await using var md5Stream = new MD5ComputingStreamWrapper(stream);
                await using var zipStream = new ZipInputStream(md5Stream);
                zipStream.IsStreamOwner = false;

                while (zipStream.GetNextEntry() is { } entry)
                {
                    token.ThrowIfCancellationRequested();

                    var newFilePath = $@"{temporaryDirectoryPath}{entry.Name}";

                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    if (entry.IsDirectory) continue;

                    if (!DoesDirectoryExist(newFilePath))
                    {
                        error = CreateDirectory(newFilePath);

                        if (error)
                        {
                            if (!error.IsSilent) ModioLog.Error?.Log($"Extraction operation for Modfile {mod.Id} aborted. Failed to create directory {newFilePath} with error {error}");

                            return error;
                        }
                    }

                    await using Stream writerStream = File.Open(newFilePath, FileMode.Create);

                    var buffer = new byte[1024 * 1024]; // 1 MB

                    var progressTracker = new ModInstallProgressTracker(mod, mod.File.ArchiveFileSize);

                    while (true)
                    {
                        int readSize = await zipStream.ReadAsync(buffer, 0, buffer.Length, token);

                        progressTracker.SetBytesRead(md5Stream.TotalBytesRead);

                        if (readSize > 0)
                            await writerStream.WriteAsync(buffer, 0, readSize, token);
                        else
                            break;
                    }
                }

                string rawMd5Hash = await md5Stream.GetMD5HashAsync();
                string actualMd5Hash = rawMd5Hash.Replace("-", "").ToLowerInvariant();

                if (actualMd5Hash != md5Hash && !string.IsNullOrEmpty(md5Hash))
                {
                    ModioLog.Error?.Log(
                        $"Error installing mod: md5 mismatch\n{actualMd5Hash} != {md5Hash}\nInstall Path: {installDirectoryPath}\nTemp Path: {temporaryDirectoryPath}\n"
                    );

                    error = new Error(ErrorCode.MD5DOES_NOT_MATCH);
                }
            }
            catch (TaskCanceledException)
            {
                error = new Error(IsShuttingDown ? ErrorCode.SHUTTING_DOWN : ErrorCode.OPERATION_CANCELLED);

                ModioLog.Verbose?.Log(
                    $"Cancelled installing mod: \nInstall Path: {installDirectoryPath}\nTemp Path: {temporaryDirectoryPath}\n"
                );

                Error cleanupError = DeleteDirectoryAndContents(temporaryDirectoryPath);

                if (cleanupError)
                    ModioLog.Message?.Log(
                        $"Error cleaning up temporary download location: {cleanupError.GetMessage()}\nAt: {temporaryDirectoryPath}"
                    );

                return error;
            }
            catch (OperationCanceledException)
            {
                error = new Error(IsShuttingDown ? ErrorCode.SHUTTING_DOWN : ErrorCode.OPERATION_CANCELLED);

                ModioLog.Verbose?.Log(
                    $"Cancelled installing mod: \nInstall Path: {installDirectoryPath}\nTemp Path: {temporaryDirectoryPath}\n"
                );

                Error cleanupError = DeleteDirectoryAndContents(temporaryDirectoryPath);

                if (cleanupError)
                    ModioLog.Message?.Log(
                        $"Error cleaning up temporary download location: {cleanupError.GetMessage()}\nAt: {temporaryDirectoryPath}"
                    );

                return error;
            }
            catch (Exception exception)
            {
                ModioLog.Error?.Log(
                    $"Error installing mod: {exception}\nInstall Path: {installDirectoryPath}\nTemp Path: {temporaryDirectoryPath}\n"
                );

                error = new Error(IsShuttingDown ? ErrorCode.SHUTTING_DOWN : ErrorCode.OPERATION_CANCELLED);
            }
            finally
            {
                OngoingTaskCount--;
            }

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Extraction operation for Modfile {mod.Id} aborted.");
                Error cleanupError = DeleteDirectoryAndContents(temporaryDirectoryPath);

                if (cleanupError)
                    ModioLog.Message?.Log(
                        $"Error cleaning up temporary download location: {cleanupError.GetMessage()}\nAt: {temporaryDirectoryPath}"
                    );

                return error;
            }

            OngoingTaskCount++;

            try
            {
                if (DoesDirectoryExist(installDirectoryPath)) 
                    DeleteDirectoryAndContents(installDirectoryPath);

                // This ensures Root/Installed exists
                string parentPath = Path.GetDirectoryName(installDirectoryPath);

                if (!DoesDirectoryExist(parentPath))
                {
                    error = CreateDirectory(parentPath);
                    
                    if (error)
                    {
                        if (!error.IsSilent) ModioLog.Error?.Log($"Install operation for Modfile {mod.Id} aborted. Failed to create directory {parentPath} with error {error}");

                        return error;
                    }
                }

                Directory.Move(temporaryDirectoryPath, installDirectoryPath);
            }
            catch (Exception exception)
            {
                ModioLog.Error?.Log(
                    $"Exception moving extracted files: {exception}\nFrom: {temporaryDirectoryPath}\nTo: {installDirectoryPath}"
                );

                return new ErrorException(exception);
            }
            finally
            {
                OngoingTaskCount--;
            }

            return error;
        }

        public virtual Task<Error> DeleteInstalledMod(Mod mod, long modfileId)
        {
            string directoryPath = GetInstallPath(mod.Id, modfileId);

            Error error = DeleteDirectoryAndContents(directoryPath);

            if (error)
                ModioLog.Error?.Log($"Error deleting installed mod {mod}: {error.GetMessage()}\nAt: {directoryPath}");

            return Task.FromResult(error);
        }

        public virtual Task<(Error error, List<(long modId, long modfileId)> results)> ScanForInstalledMods()
        {
            try
            {
                var validPaths = new List<(long modId, long modfileId)>();

                foreach ((Error error, string path) in IterateDirectoriesInDirectory(Path.Combine(Root, "mods")))
                {
                    if (error) continue;

                    string fileName = Path.GetFileName(path);
                    string[] nameComponents = fileName.Split('_');

                    if (nameComponents.Length == 2 &&
                        long.TryParse(nameComponents[0], out long modId) &&
                        long.TryParse(nameComponents[1], out long modfileId))
                        validPaths.Add((modId, modfileId));
                    else
                        ModioLog.Message?.Log($"Invalid Install name: [{fileName}], skipping");
                }

                return Task.FromResult((Error.None, validPaths));
            }
            catch (Exception exception)
            {
                ModioLog.Error?.Log($"Exception scanning Mod Installations: {exception}");

                return Task.FromResult(((Error)new ErrorException(exception), new List<(long, long)>()));
            }
        }

        protected virtual void MigrateLegacyModInstalls()
        {
            MigrateLegacyModInstalls(Path.Combine(Root, "Installed"));
        }

        protected void MigrateLegacyModInstalls(string legacyDirectoryPath)
        {
            try
            {
                foreach ((Error error, string legacyPath) in IterateDirectoriesInDirectory(legacyDirectoryPath))
                {
                    if (error) continue;

                    string fileName = Path.GetFileName(legacyPath);
                    string[] nameComponents = fileName.Split('_');

                    if (nameComponents.Length != 2
                        || !long.TryParse(nameComponents[0], out long modId)
                        || !long.TryParse(nameComponents[1], out long modfileId))
                    {
                        ModioLog.Message?.Log($"Invalid Install name in legacy folder: [{fileName}], skipping");
                        continue;
                    }

                    string newPath = GetInstallPath(modId, modfileId);

                    if (Directory.Exists(newPath))
                    {
                        ModioLog.Message?.Log($"Deleting redundant legacy folder: {newPath}");
                        Directory.Delete(legacyPath, true);
                    }
                    else
                    {
                        ModioLog.Message?.Log($"Moving legacy folder: {legacyPath} to {newPath}");
                        //Ensure the parent directory exists
                        CreateDirectory(Path.GetFullPath(Path.Combine(newPath, "..") + Path.DirectorySeparatorChar));
                        Directory.Move(legacyPath, newPath);
                    }
                }
            }
            catch (Exception exception)
            {
                ModioLog.Error?.Log($"Exception scanning legacy Mod Installations: {exception}");
            }
        }

#endregion

#region Images

        // Paths can easily be replaced with Uris that FileIO then generates a filepath with
        public virtual async Task<(Error error, byte[] result)> ReadCachedImage(Uri serverPath)
        {
            if (serverPath == null) return (new Error(ErrorCode.BAD_PARAMETER), null);

            string path = GetImageDataFilePath(serverPath);

            Error error;
            byte[] output = Array.Empty<byte>();

            if (!IsValidPath(path)) return (new Error(ErrorCode.BAD_PARAMETER), output);
            if (!DoesFileExist(path)) return (new Error(ErrorCode.FILE_NOT_FOUND), output);

            (error, output) = await ReadFile(path);

            if (error) ModioLog.Warning?.Log($"Error reading image: {error.GetMessage()}\nAt: {path}");

            return (error, output);
        }

        public virtual async Task<Error> WriteCachedImage(Uri serverPath, byte[] data)
        {
            if (serverPath == null) return new Error(ErrorCode.BAD_PARAMETER);

            string path = GetImageDataFilePath(serverPath);

            if (!IsValidPath(path) || data == null) return new Error(ErrorCode.BAD_PARAMETER);

            Error error = await WriteFile(path, data);

            if (error) ModioLog.Warning?.Log($"Error writing image: {error.GetMessage()}\nAt: {path}");

            return error;
        }

        public virtual Task<Error> DeleteCachedImage(Uri serverPath)
        {
            if (serverPath == null) return Task.FromResult(new Error(ErrorCode.BAD_PARAMETER));

            string path = GetImageDataFilePath(serverPath);

            Error error = DeleteFile(path);

            if (error) ModioLog.Warning?.Log($"Error deleting image: {error.GetMessage()}\nAt: {path}");

            return Task.FromResult(error);
        }

#endregion

#region Drive Space

        public virtual Task<bool> IsThereAvailableFreeSpaceFor(long tempBytes, long persistentBytes)
            => Task.FromResult(IsThereEnoughDiskSpaceFor(tempBytes + persistentBytes));
        //Either download plus install, or temp extracted plus copy

        public virtual Task<bool> IsThereAvailableFreeSpaceForModfile(long bytes)
            => Task.FromResult(IsThereEnoughDiskSpaceFor(bytes));

        public virtual Task<long> GetAvailableFreeSpaceForModfile() => Task.FromResult(GetAvailableFreeSpace());

        public virtual Task<bool> IsThereAvailableFreeSpaceForModInstall(long bytes)
            => Task.FromResult(IsThereEnoughDiskSpaceFor(bytes));

        public virtual Task<long> GetAvailableFreeSpaceForModInstall() => Task.FromResult(GetAvailableFreeSpace());

#endregion

        //Theoretically we can check the mod class rather than needing to do this
        protected virtual async Task<bool> IsThereEnoughSpaceForExtracting(string archiveFilePath)
        {
            if (!DoesFileExist(archiveFilePath)) return new Error(ErrorCode.FILE_NOT_FOUND);

            await using Stream fileStream = File.Open(archiveFilePath, FileMode.Open);
            await using var stream = new ZipInputStream(fileStream);
            long uncompressedSize = 0;

            while (stream.GetNextEntry() is { } entry)
                if (entry.Size == -1)
                    ModioLog.Verbose?.Log($"Size unknown for file in zip: [{entry.Name}]");
                else
                    uncompressedSize += entry.Size;

            return await IsThereAvailableFreeSpaceForModInstall(uncompressedSize);
        }

        protected virtual bool IsThereEnoughDiskSpaceFor(long bytes)
        {
            var spaceAvailable = GetAvailableFreeSpace();
            return spaceAvailable <= 0 || bytes < spaceAvailable;
        }

        protected virtual long GetAvailableFreeSpace()
        {
            if (ModioClient.Settings.TryGetPlatformSettings(out ModioDiskTestSettings settings)
                && settings.OverrideDiskSpaceRemaining)
                return settings.BytesRemaining;

            //plugin likely isn't initialized yet
            if (!Initialized) return 0;
            
            // IL2CPP does not support DriveInfo.AvailableFreeSpace. Because of this, we have to implement our own
            // methods of checking storage for each platform
            
#if ENABLE_IL2CPP && (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            GetDiskFreeSpaceEx(Root, out ulong availableBytesUlong, out _, out _);
            long availableBytes = (long)availableBytesUlong;
#else
            var drive = new DriveInfo(Path.GetPathRoot(Root));
            long availableBytes = drive.AvailableFreeSpace;
#endif
            return availableBytes;
        }
        
#if ENABLE_IL2CPP && (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetDiskFreeSpaceEx(
            string directory,
            out ulong freeBytesAvailable,
            out ulong totalNumberOfBytes,
            out ulong totalNumberOfFreeBytes
        );
#endif

        protected virtual bool IsValidPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            try
            {
                string fullPath = Path.GetFullPath(filePath);
                string root = Path.GetPathRoot(fullPath);

                return root == "/" || !string.IsNullOrEmpty(root.Trim('\\', '/'));
            }
            catch
            {
                return false;
            }
        }

        protected virtual bool DoesDirectoryExist(string filePath) => Directory.Exists(filePath);

        protected virtual bool DoesFileExist(string filePath) => File.Exists(filePath);

        protected virtual Error CreateDirectory(string filePath)
        {
            if (!IsValidPath(filePath)) return new Error(ErrorCode.BAD_PARAMETER);

            string directoryPath = Path.GetDirectoryName(filePath);

            if (string.IsNullOrEmpty(directoryPath)) return new Error(ErrorCode.BAD_PARAMETER);

            try
            {
                Directory.CreateDirectory(directoryPath);
            }
            catch (Exception exception)
            {
                return new ErrorException(exception);
            }

            return Error.None;
        }

        protected virtual Error DeleteDirectoryAndContents(string filePath)
        {
            if (!IsValidPath(filePath)) return new Error(ErrorCode.BAD_PARAMETER);
            if (!DoesDirectoryExist(filePath)) return Error.None;

            try
            {
                Directory.Delete(filePath, true);
            }
            catch (Exception exception)
            {
                return new ErrorException(exception);
            }

            return Error.None;
        }

        protected virtual Error DeleteFile(string filePath)
        {
            if (!IsValidPath(filePath)) return new Error(ErrorCode.BAD_PARAMETER);
            if (!DoesFileExist(filePath)) return Error.None;

            try
            {
                File.Delete(filePath);
            }
            catch (Exception exception)
            {
                return new ErrorException(exception);
            }

            return Error.None;
        }

#region File Read/Write

        protected virtual async Task<Error> WriteFile(string path, byte[] data)
        {
            if (!IsValidPath(path) || data == null) return new Error(ErrorCode.BAD_PARAMETER);

            Error error = CreateDirectory(path);
            if (error) return error;

            OngoingTaskCount++;

            try
            {
                await using FileStream fileStream = File.Open(path, FileMode.Create);
                fileStream.Position = 0;
                await fileStream.WriteAsync(data, 0, data.Length);

                return Error.None;
            }
            catch (Exception exception)
            {
                return new ErrorException(exception);
            }
            finally
            {
                OngoingTaskCount--;
            }
        }

        protected virtual async Task<(Error error, byte[] result)> ReadFile(string path)
        {
            byte[] output = Array.Empty<byte>();

            if (!IsValidPath(path)) return (new Error(ErrorCode.BAD_PARAMETER), output);
            if (!DoesFileExist(path)) return (new Error(ErrorCode.FILE_NOT_FOUND), output);

            OngoingTaskCount++;

            try
            {
                await using FileStream fileStream = File.Open(path, FileMode.Open);
                output = new byte[fileStream.Length];
                _ = await fileStream.ReadAsync(output, 0, output.Length);

                return (Error.None, output);
            }
            catch (Exception exception)
            {
                return (new ErrorException(exception), output);
            }
            finally
            {
                OngoingTaskCount--;
            }
        }

        protected virtual async Task<Error> WriteTextFile(string path, string data)
        {
            if (!IsValidPath(path) || string.IsNullOrEmpty(data)) return new Error(ErrorCode.BAD_PARAMETER);

            (Error error, byte[] result) = ConvertUTF8Data(data);

            if (error) return error;

            return await WriteFile(path, result);
        }

        protected virtual async Task<(Error error, string result)> ReadTextFile(string path)
        {
            var output = string.Empty;

            if (ShutdownToken.IsCancellationRequested) return (new Error(ErrorCode.SHUTTING_DOWN), output);
            if (!IsValidPath(path)) return (new Error(ErrorCode.BAD_PARAMETER), output);
            if (!DoesFileExist(path)) return (new Error(ErrorCode.FILE_NOT_FOUND), output);

            (Error error, byte[] data) = await ReadFile(path);
            if (!error) (error, output) = TryParseUTF8Data(data);

            return (error, output);
        }

#endregion

#region Encoding

        protected virtual (Error error, string result) TryParseUTF8Data(byte[] data)
        {
            if (data == null) return (new Error(ErrorCode.BAD_PARAMETER), string.Empty);

            try
            {
                string output = Encoding.UTF8.GetString(data);
                return (Error.None, output);
            }
            catch (Exception exception)
            {
                ModioLog.Error?.Log($"Exception parsing bytes to string: {exception}");
                return (new ErrorException(exception), string.Empty);
            }
        }

        protected virtual (Error error, byte[] result) ConvertUTF8Data(string data)
        {
            if (string.IsNullOrEmpty(data)) return (new Error(ErrorCode.BAD_PARAMETER), Array.Empty<byte>());

            try
            {
                byte[] output = Encoding.UTF8.GetBytes(data);
                return (Error.None, output);
            }
            catch (Exception exception)
            {
                ModioLog.Error?.Log($"Exception parsing string to bytes: {exception}");
                return (new Error(ErrorCode.BAD_PARAMETER), Array.Empty<byte>());
            }
        }

#endregion

#region Paths

        public virtual string GetModfilePath(long modId, long modfileId)
            => Path.Combine(Root, "Modfiles", $"{modId}_{modfileId}_modfile.zip");

        public virtual string GetInstallPath(long modId, long modfileId)
            // Match V2 install path. Note that initial V3 versions put mods in an "Installed" directory; see MigrateLegacyModInstalls
            => $"{Path.Combine(Root, "mods", $"{modId}_{modfileId}")}{Path.DirectorySeparatorChar}";

        protected virtual string GetTemporaryInstallPath(long modId, long modfileId)
            => $"{Path.Combine(Root, "Temp", $"{modId}_{modfileId}")}{Path.DirectorySeparatorChar}";

        protected virtual string GetGameDataFilePath()
            => Path.Combine(Root, $"{GameId}_game_data.json");

        protected virtual string GetIndexFilePath() 
            => Path.Combine(Root, $"{GameId}_mod_index.json");

        protected virtual string GetUserDataFilePath(string localUserId)
            => Path.Combine(UserRoot, $"{localUserId}_user_data.json");

        protected virtual string GetImageDataFilePath(Uri serverPath)
            => Path.Combine(Root, $"ImageCache{serverPath.LocalPath}");

#endregion

        public virtual bool DoesModfileExist(long modId, long modfileId)
        {
            string filePath = GetModfilePath(modId, modfileId);
            return DoesFileExist(filePath);
        }

        public virtual bool DoesInstallExist(long modId, long modfileId)
        {
            string directoryPath = GetInstallPath(modId, modfileId);
            return DoesDirectoryExist(directoryPath);
        }

        public virtual async Task<Error> CompressToZip(string filePath, Stream outputTo)
        {
            if (!DoesDirectoryExist(filePath) && !DoesFileExist(filePath)) return new Error(ErrorCode.FILE_NOT_FOUND);

            Error returnError = Error.None;

            // So the substring is reliable we get the full path here
            filePath = Path.GetFullPath(filePath);

            await using var zipStream = new ZipOutputStream(outputTo);

            foreach ((Error error, string fileName) in IterateFilesInDirectory(filePath))
            {
                if (error) continue;

                await using FileStream fileStream = File.Open(fileName, FileMode.Open);
                string entryName = Path.GetFullPath(fileName).Substring(filePath.Length);
                
                if (string.IsNullOrEmpty(entryName)) 
                    entryName = Path.GetFileName(filePath);

                await CompressStream(entryName, fileStream, zipStream);
            }

            return returnError;
        }

        protected virtual async Task CompressStream(string entryName, Stream stream, ZipOutputStream zipStream)
        {
            var newEntry = new ZipEntry(entryName);

            zipStream.PutNextEntry(newEntry);

            //Use this if we don't need progress tracking, otherwise use the block below
            // (or just track stream.Position/stream.Length while waiting on this task)
            await stream.CopyToAsync(zipStream, 4096);

            /*
            byte[] data = new byte[4096];
            long max = stream.Length;
            stream.Position = 0;

            while(stream.Position < stream.Length)
            {
                int size = await stream.ReadAsync(data, 0, data.Length);

                if (size <= 0)
                    break;

                await zipStream.WriteAsync(data, 0, size);

                if(progressHandle != null)
                {
                    // This is only the progress for the current entry
                    progressHandle.Progress = stream.Position / (float)max;
                }
            }*/

            zipStream.CloseEntry();
        }

        protected virtual IEnumerable<(Error error, string fileName)> IterateFilesInDirectory(string directoryPath)
        {
            if (!DoesDirectoryExist(directoryPath))
            {
                if (DoesFileExist(directoryPath))
                    yield return (Error.None, directoryPath);
                else
                    yield return (new Error(ErrorCode.FILE_NOT_FOUND), null);

                yield break;
            }

            const string AllFilesFilter = "*";

            foreach (string file in Directory.EnumerateFiles(
                         directoryPath,
                         AllFilesFilter,
                         SearchOption.AllDirectories
                     ))
                yield return (Error.None, file);
        }

        protected virtual IEnumerable<(Error error, string directoryPath)> IterateDirectoriesInDirectory(
            string directoryPath
        )
        {
            if (!DoesDirectoryExist(directoryPath))
            {
                yield return (new Error(ErrorCode.FILE_NOT_FOUND), null);
                yield break;
            }

            foreach (string file in Directory.EnumerateDirectories(directoryPath)) yield return (Error.None, file);
        }
    }
}
