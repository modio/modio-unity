using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Errors;

namespace Modio.Mods.Builder
{
    public class ModfileBuilder
    {
        public string FilePath { get; private set; } = null;
        public string Version { get; private set; } = null;
        public string ChangeLog { get; private set; } = null;
        public string MetadataBlob { get; private set; } = null;
        public Platform[] Platforms { get; private set; } = null;

        ModId ParentId => _parentModBuilder.EditTarget.Id;
        readonly ModBuilder _parentModBuilder;
        
        internal ModfileBuilder(ModBuilder parent) => _parentModBuilder = parent;

        /// <remarks>This should be a directory</remarks>
        public ModfileBuilder SetSourceDirectoryPath(string filePath)
        {
            FilePath = filePath;
            return this;
        }
        
        public ModfileBuilder SetVersion(string version)
        {
            Version = version;
            return this;
        }
        
        public ModfileBuilder SetChangelog(string changelog)
        {
            ChangeLog = changelog;
            return this;
        }
        
        public ModfileBuilder SetMetadataBlob(string metadataBlob)
        {
            MetadataBlob = metadataBlob;
            return this;
        }

        /// <remarks>This will overwrite all platforms on this modfile.</remarks>
        public ModfileBuilder SetPlatform(Platform platform) => SetPlatforms(new[] { platform, });

        /// <remarks>This will overwrite all platforms on this modfile.</remarks>
        public ModfileBuilder SetPlatforms(ICollection<Platform> platforms)
        {
            Platforms = platforms.ToArray();
            return this;
        }

        public ModfileBuilder AppendPlatform(Platform platform) => AppendPlatforms(new[] { platform, });

        public ModfileBuilder AppendPlatforms(ICollection<Platform> platforms)
        {
            Platforms = Platforms.Concat(platforms).ToArray();
            return this;
        }

        public ModBuilder FinishModfile() => _parentModBuilder;
        
        internal async Task<Error> PublishModfile()
        {
            if (_parentModBuilder.EditTarget is null)
            {
                ModioLog.Error?.Log($"Unable to publish modfile, no {typeof(ModId)} found to upload to. How did you get here?");
                return new Error(ErrorCode.BAD_PARAMETER);
            }
            
            if (!Directory.Exists(FilePath))
            {
                ModioLog.Error?.Log($"Unable to publish modfile, directory {FilePath} not found");
                return new Error(ErrorCode.BAD_PARAMETER);
            }
            
            string temporaryDirectoryPath = ModioClient.DataStorage.GetInstallPath(ParentId, 0);
            Directory.CreateDirectory(temporaryDirectoryPath);
            var temporaryFilePath = Path.Combine(temporaryDirectoryPath, "upload.zip");
            Error error;

            await using (Stream writerStream = File.Open(temporaryFilePath, FileMode.Create))
            {
                error = await ModioClient.DataStorage.CompressToZip(FilePath, writerStream);
                if (error) 
                    return error;
            }

            long fileSize = new FileInfo(temporaryFilePath).Length;

            ModfileObject? modfileObject;
            const long oneHundredMiB = 100 * 1024 * 1024;

            if (fileSize > oneHundredMiB) //recommended limit
            {
                await using Stream readStream = File.Open(temporaryFilePath, FileMode.Open);
                error = await AddMultipartModfile(readStream);
            }
            else
            {
                var modioAPIFileParameter = new ModioAPIFileParameter()
                {
                    Name = "upload.zip",
                    Path = temporaryFilePath
                };

                string[] platformStrings = Platforms.Select(GetPlatformHeader).ToArray();
                
                var addModfileRequest = new AddModfileRequest(
                    modioAPIFileParameter,
                    Version,
                    ChangeLog,
                    MetadataBlob,
                    platformStrings,
                    null
                );

                var uploadTask = ModioAPI.Files.AddModfile(ParentId, addModfileRequest);

                (error, _) = await uploadTask;
            }
            
            return error;
        }
        
        async Task<Error> AddMultipartModfile(Stream readStream)
        {
            var nonce = $"{ParentId}_{readStream.Length}_{DateTime.UtcNow.Ticks}";

            (Error error, MultipartUploadObject? multipartUploadObject) session =
                await ModioAPI.FilesMultipartUploads.CreateMultipartUploadSession(
                    ParentId,
                    new CreateMultipartUploadSessionRequest("upload.zip", nonce)
                );

            if (session.error) return session.error;
            
            if (!session.multipartUploadObject.HasValue)
                return new Error(ErrorCode.NO_DATA_AVAILABLE);

            string uploadId = session.multipartUploadObject.Value.UploadId;

            Error error = await AddAllMulipartUploadParts(uploadId,  0, readStream);

            if (error) return error;

            if (session.error) return session.error;

            (Error error, MultipartUploadObject? multipartUploadObject) end =
                await ModioAPI.FilesMultipartUploads.CompleteMultipartUploadSession(
                    uploadId,
                    ParentId
                );

            if (end.error) return end.error;
            
            string[] platformStrings = Platforms.Select(GetPlatformHeader).ToArray();

            (Error error, ModfileObject? modfileObject) upload = await ModioAPI.Files.AddModfile(
                ParentId,
                new AddModfileRequest(ModioAPIFileParameter.None, Version, ChangeLog, MetadataBlob, platformStrings, uploadId)
            );

            return Error.None;
        }
        
        async Task<Error> AddAllMulipartUploadParts(
            string uploadId,
            int partCount,
            Stream readStream
        )
        {
            const int fiftyMiB = 52_428_800; //max chunkSize
            int chunkSize = fiftyMiB;
            int endByte = chunkSize - 1; //last byte of a chunk
            int startByte = fiftyMiB * partCount;

            if (readStream.CanSeek) readStream.Position = startByte;

            var buffer = new byte[chunkSize];

            while (await readStream.ReadAsync(buffer, 0, chunkSize) > 0)
            {
                byte[] data;

                if (endByte >= readStream.Length)
                {
                    endByte = (int)readStream.Length - 1; //adjust end byte to match the last byte of the zip file
                }

                //shrink byte array if last part
                if (endByte + 1 - startByte < chunkSize)
                {
                    data = new byte[endByte + 1 - startByte];
                    Array.Copy(buffer, data, endByte + 1 - startByte);
                }
                else
                {
                    data = buffer;
                }

                (Error error, MultipartUploadPartObject? multipartUploadPartObject) part =
                    await ModioAPI.FilesMultipartUploads.AddMultipartUploadPart(
                        uploadId,
                        ParentId,
                        $"bytes {startByte}-{endByte}/{readStream.Length}",
                        data
                    );

                if (part.error)
                {
                    return part.error;
                }

                startByte = endByte + 1;
                endByte = startByte + chunkSize - 1;
            }

            return Error.None;
        }
        
        async Task<(Error, ModfileObject?)> RetryAddMultipartModfile(
            string uploadId,
            string version,
            string changelog,
            string metadataBlob,
            string[] platforms,
            Stream readStream
        )
        {
            const int pageSize = 410; // approximate max for an upload 

            var filter = new ModioAPI.FilesMultipartUploads.GetMultipartUploadPartsFilter(0, pageSize, uploadId);

            (Error error, Pagination<MultipartUploadPartObject[]>? multipartUploadPartObjects) getPartsResponse =
                await ModioAPI.FilesMultipartUploads.GetMultipartUploadParts(ParentId, filter);

            if (getPartsResponse.error) return (getPartsResponse.error, null);

            if (!getPartsResponse.multipartUploadPartObjects.HasValue)
                return (new Error(ErrorCode.NO_DATA_AVAILABLE), null);

            int partCount = getPartsResponse.multipartUploadPartObjects.Value.Data.Length;

            Error error = await AddAllMulipartUploadParts(uploadId, partCount, readStream);

            if (error) return (error, null);

            (Error error, MultipartUploadObject? multipartUploadObject) end =
                await ModioAPI.FilesMultipartUploads.CompleteMultipartUploadSession(
                    uploadId,
                    ParentId
                );

            if (end.error) return (end.error, null);

            (Error error, ModfileObject? modfileObject) upload = await ModioAPI.Files.AddModfile(
                ParentId,
                new AddModfileRequest(ModioAPIFileParameter.None, version, changelog, metadataBlob, platforms, uploadId)
            );

            return (Error.None, upload.modfileObject);
        }

        public enum Platform
        {
            Windows,
            Mac,
            Linux,
            Android,
            IOS,
            XboxOne,
            XboxSeriesX,
            PlayStation4,
            PlayStation5,
            Switch,
            Oculus,
        }

        static string GetPlatformHeader(Platform platform) => platform switch
        {
            Platform.Windows      => "windows",
            Platform.Mac          => "mac",
            Platform.Linux        => "linux",
            Platform.Android      => "android",
            Platform.IOS          => "ios",
            Platform.XboxOne      => "xboxone",
            Platform.XboxSeriesX  => "xboxseriesx",
            Platform.PlayStation4 => "ps4",
            Platform.PlayStation5 => "ps5",
            Platform.Switch       => "switch",
            Platform.Oculus       => "oculus",
            _                     => string.Empty,
        };
    }
}
