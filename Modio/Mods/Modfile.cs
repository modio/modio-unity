using Modio.API.SchemaDefinitions;

namespace Modio.Mods
{
    public class Modfile
    {
        public long ModId { get; private set; }
        public long Id { get; private set; }
        public long FileSize { get; private set; }
        public long ArchiveFileSize { get; private set; }
        public string InstallLocation { get; internal set; }
        public string Version { get; private set; }
        public string MetadataBlob { get; private set; }
        public ModFileState State { get; internal set; }
        public Error FileStateErrorCause { get; internal set; } = Error.None;
        public float FileStateProgress { get; internal set; }
        public long DownloadingBytesPerSecond { get; internal set; }

        internal Modfile(ModfileObject modfileObject)
        {
            ApplyDetailsFromModfileObject(modfileObject);
        }

        internal void ApplyDetailsFromModfileObject(ModfileObject modfileObject)
        {
            ModId = modfileObject.ModId;
            Id = modfileObject.Id;
            FileSize = modfileObject.FilesizeUncompressed;
            ArchiveFileSize = modfileObject.Filesize;
            Version = modfileObject.Version;
            MetadataBlob = modfileObject.MetadataBlob;
        }
    }
}
