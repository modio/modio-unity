// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject(MemberSerialization.Fields)]
    internal readonly partial struct ModfileObject 
    {
        /// <summary>Unique modfile id.</summary>
        internal readonly long Id;
        /// <summary>Unique mod id.</summary>
        internal readonly long ModId;
        /// <summary>Unix timestamp of date file was added.</summary>
        internal readonly long DateAdded;
        /// <summary>Unix timestamp of date file was updated.</summary>
        internal readonly long DateUpdated;
        /// <summary>Unix timestamp of date file was virus scanned.</summary>
        internal readonly long DateScanned;
        /// <summary>Current virus scan status of the file. For newly added files that have yet to be scanned this field will change frequently until a scan is complete:<br/><br/>__0__ = Not scanned<br/>__1__ = Scan complete<br/>__2__ = In progress<br/>__3__ = Too large to scan<br/>__4__ = File not found<br/>__5__ = Error Scanning</summary>
        internal readonly long VirusStatus;
        /// <summary>Was a virus detected:<br/><br/>__0__ = No threats detected<br/>__1__ = Flagged as malicious<br/>__2__ = Flagged as containing potentially harmful files (i.e. EXEs)</summary>
        internal readonly long VirusPositive;
        /// <summary>Deprecated: No longer used and will be removed in subsequent API version.</summary>
        internal readonly string VirustotalHash;
        /// <summary>Size of the file in bytes.</summary>
        internal readonly long Filesize;
        /// <summary>The uncompressed filesize of the zip archive.</summary>
        internal readonly long FilesizeUncompressed;
        /// <summary>Contains a dictionary of filehashes for the contents of the download.</summary>
        internal readonly FilehashObject Filehash;
        /// <summary>Filename including extension.</summary>
        internal readonly string Filename;
        /// <summary>Release version this file represents.</summary>
        internal readonly string Version;
        /// <summary>Changelog for the file.</summary>
        internal readonly string Changelog;
        /// <summary>Metadata stored by the game developer for this file.</summary>
        internal readonly string MetadataBlob;
        /// <summary>Contains download data for the modfile.</summary>
        internal readonly DownloadObject Download;
        /// <summary>Contains modfile platform data.</summary>
        internal readonly ModfilePlatformObject[] Platforms;

        /// <param name="id">Unique modfile id.</param>
        /// <param name="modId">Unique mod id.</param>
        /// <param name="dateAdded">Unix timestamp of date file was added.</param>
        /// <param name="dateUpdated">Unix timestamp of date file was updated.</param>
        /// <param name="dateScanned">Unix timestamp of date file was virus scanned.</param>
        /// <param name="virusStatus">Current virus scan status of the file. For newly added files that have yet to be scanned this field will change frequently until a scan is complete:<br/><br/>__0__ = Not scanned<br/>__1__ = Scan complete<br/>__2__ = In progress<br/>__3__ = Too large to scan<br/>__4__ = File not found<br/>__5__ = Error Scanning</param>
        /// <param name="virusPositive">Was a virus detected:<br/><br/>__0__ = No threats detected<br/>__1__ = Flagged as malicious<br/>__2__ = Flagged as containing potentially harmful files (i.e. EXEs)</param>
        /// <param name="virustotalHash">Deprecated: No longer used and will be removed in subsequent API version.</param>
        /// <param name="filesize">Size of the file in bytes.</param>
        /// <param name="filesizeUncompressed">The uncompressed filesize of the zip archive.</param>
        /// <param name="filehash">Contains a dictionary of filehashes for the contents of the download.</param>
        /// <param name="filename">Filename including extension.</param>
        /// <param name="version">Release version this file represents.</param>
        /// <param name="changelog">Changelog for the file.</param>
        /// <param name="metadataBlob">Metadata stored by the game developer for this file.</param>
        /// <param name="download">Contains download data for the modfile.</param>
        /// <param name="platforms">Contains modfile platform data.</param>
        [JsonConstructor]
        public ModfileObject(
            long id,
            long mod_id,
            long date_added,
            long date_updated,
            long date_scanned,
            long virus_status,
            long virus_positive,
            string virustotal_hash,
            long filesize,
            long filesize_uncompressed,
            FilehashObject filehash,
            string filename,
            string version,
            string changelog,
            string metadata_blob,
            DownloadObject download,
            ModfilePlatformObject[] platforms
        ) {
            Id = id;
            ModId = mod_id;
            DateAdded = date_added;
            DateUpdated = date_updated;
            DateScanned = date_scanned;
            VirusStatus = virus_status;
            VirusPositive = virus_positive;
            VirustotalHash = virustotal_hash;
            Filesize = filesize;
            FilesizeUncompressed = filesize_uncompressed;
            Filehash = filehash;
            Filename = filename;
            Version = version;
            Changelog = changelog;
            MetadataBlob = metadata_blob;
            Download = download;
            Platforms = platforms;
        }
    }
}
