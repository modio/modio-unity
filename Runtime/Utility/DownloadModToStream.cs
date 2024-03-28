using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using ModIO.Implementation;
using ModIO.Implementation.API;
using ModIO.Implementation.API.Objects;
using Logger = ModIO.Implementation.Logger;

namespace ModIO.Util
{
    /// <summary>
    /// <inheritdoc cref="Utility.DownloadModToStream"/>
    /// </summary>
    public class DownloadModToStreamOperation : IDisposable
    {
        public enum OperationStatus
        {
            RequestingModInfo,
            Downloading,
            Verifying,
            ProcessingArchive,
            Cancelled,
            Succeeded,
            Failed,
        }

        public readonly ModId modId;
        public readonly Stream archiveStream;
        public bool closeStream;

        public readonly Task task;
        public OperationStatus Status { get; private set; }= OperationStatus.RequestingModInfo;
        public bool IsDownloading => Status == OperationStatus.Downloading;
        public bool IsCompleted => Status >= OperationStatus.Cancelled;
        public bool IsCancelled => Status == OperationStatus.Cancelled;
        public bool IsSucceeded => Status == OperationStatus.Succeeded;
        public bool IsFailed => IsCancelled || Status == OperationStatus.Failed;

        ModfileObject modfileObject;

        public float DownloadProgress { get; private set; }

        readonly Dictionary<string, ArchiveStreamFile> filesByName = new Dictionary<string, ArchiveStreamFile>();

        public string FailedMessage { get; private set; }

        readonly Stopwatch stopwatch = new Stopwatch();
        readonly byte[] buffer = new byte[1024 * 1024]; // 1MB

        internal DownloadModToStreamOperation(ModId modId, Stream archiveStream, bool closeStream)
        {
            this.modId = modId;
            this.archiveStream = archiveStream;

            if (archiveStream.CanWrite && archiveStream.CanRead && archiveStream.CanSeek)
                task = Init();
            else
            {
                task = Task.CompletedTask;
                SetFailed("Stream must support writing, reading and seeking.");
            }
        }

        async Task Init()
        {
            modfileObject = await RequestModInfo();
            if (IsFailed || modfileObject.download.binary_url == null)
                return;

            await DownloadModfile();
            if (IsFailed) return;

            await VerifyDownload();
            if (IsFailed) return;

            await ProcessArchive();
            if (IsFailed) return;

            Logger.Log(LogLevel.Message, $"Downloaded mod [{modId}_{modfileObject.id}] to [{archiveStream.GetType()}]");
            Status = OperationStatus.Succeeded;
        }

        async Task<ModfileObject> RequestModInfo()
        {
            ResultAnd<ModObject> result = await Implementation.API.Requests.GetMod.Request(modId).RunViaWebRequestManager();

            if (result.result.Succeeded())
                return result.value.modfile;

            SetFailed(result.result.message);
            return default;
        }

        async Task DownloadModfile()
        {
            Logger.Log(LogLevel.Message, $"Downloading mod [{modId}_{modfileObject.id}] to [{archiveStream.GetType()}]");

            Status = OperationStatus.Downloading;

            ProgressHandle progressHandle = new ProgressHandle();

            RequestHandle<Result> requestHandle = WebRequestManager.Download(modfileObject.download.binary_url, archiveStream, progressHandle);

            while (!requestHandle.task.IsCompleted)
            {
                if (IsCancelled)
                {
                    requestHandle.cancel();
                    return;
                }

                DownloadProgress = progressHandle.Progress;
                await Task.Yield();
            }

            DownloadProgress = progressHandle.Progress;

            if (requestHandle.task.Result.Succeeded())
                return;

            SetFailed(requestHandle.task.Result.message);
            Logger.Log(LogLevel.Error, $"Failed to download mod [{modId}_{modfileObject.id}]");
        }

        async Task VerifyDownload()
        {
            Status = OperationStatus.Verifying;

            archiveStream.Position = 0;
            string md5 = await IOUtil.GenerateMD5Async(archiveStream);

            if (md5.Equals(modfileObject.filehash.md5))
                return;

            SetFailed("Failed to validate downloaded file with MD5. The download may have been corrupted, please try again.");
            Logger.Log(LogLevel.Error, $"Failed to download mod [{modId}_{modfileObject.id}]");
        }

        async Task ProcessArchive()
        {
            Status = OperationStatus.ProcessingArchive;

            stopwatch.Restart();

            archiveStream.Position = 0;
            using ZipInputStream stream = new ZipInputStream(archiveStream);
            stream.IsStreamOwner = false;

            while (stream.GetNextEntry() is { } entry)
            {
                if (stopwatch.ElapsedMilliseconds >= 15)
                {
                    await Task.Yield();
                    stopwatch.Restart();
                }

                if (entry.IsDirectory || entry.Name.Contains("__MACOSX"))
                    continue;

                filesByName[entry.Name] = new ArchiveStreamFile(entry);
            }

            stopwatch.Stop();
        }

        void SetFailed(string message)
        {
            Status = OperationStatus.Failed;
            FailedMessage = message;
        }

        public void Cancel()
        {
            Status = OperationStatus.Cancelled;
            FailedMessage = "Operation cancelled";
        }

        /// <summary>Returns a new array of all file entries in the archive.<br />Use <see cref="ExtractFileToStream(DownloadModToStreamOperation.ArchiveStreamFile,Stream,bool)"/> for extracting files.</summary>
        public IEnumerable<ArchiveStreamFile> GetFiles() => filesByName.Values.ToArray();

        /// <summary>Extracts a file from the archive and optionally removes it.<br />Use <see cref="GetFiles"/> to get a list of files in the archive.</summary>
        public async Task ExtractFileToStream(ArchiveStreamFile file, Stream result, bool removeFromArchive = false)
        {
            await ExtractFileToStream(file.path, result, removeFromArchive);
        }

        /// <summary>Extracts a file from the archive and optionally removes it.<br />Use <see cref="GetFiles"/> to get a list of files in the archive.</summary>
        public async Task ExtractFileToStream(string path, Stream result, bool removeFromArchive = false)
        {
            stopwatch.Restart();

            using ZipFile file = new ZipFile(archiveStream);
            file.IsStreamOwner = false;

            ZipEntry entry = file.GetEntry(path);
            if (entry == null)
            {
                Logger.Log(LogLevel.Error, $"File '{path}' does not exist in archive.");
                return;
            }

            Stream inputStream = file.GetInputStream(entry);
            int bytesRead;

            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                if (stopwatch.ElapsedMilliseconds >= 15)
                {
                    await Task.Yield();
                    stopwatch.Restart();
                }

                await result.WriteAsync(buffer, 0, bytesRead);

                if (stopwatch.ElapsedMilliseconds < 15)
                {
                    await Task.Yield();
                    stopwatch.Restart();
                }
            }

            stopwatch.Stop();

            if (removeFromArchive)
                file.Delete(path);
        }

        public void Dispose()
        {
            if (closeStream)
                archiveStream?.Dispose();
        }

        public readonly struct ArchiveStreamFile
        {
            /// <summary>Relative path to the file in the archive.</summary>
            public readonly string path;
            /// <summary>The file name and extension.</summary>
            public readonly string fileName;
            public readonly long sizeCompressed;
            public readonly long sizeUncompressed;

            internal ArchiveStreamFile(ZipEntry zipEntry)
            {
                path = zipEntry.Name;
                fileName = Path.GetFileName(zipEntry.Name);
                sizeCompressed = zipEntry.CompressedSize;
                sizeUncompressed = zipEntry.Size;
            }
        }
    }
}
