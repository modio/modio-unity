using System;

namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct ModfileObject 
    {
        public long id;
        public long mod_id;
        public long date_added;
        public long date_scanned;
        public int virus_status;
        public int virus_positive;
        public string virustotal_hash;
        public long filesize;
        public FilehashObject filehash;
        public string filename;
        public string version;
        public string changelog;
        public string metadata_blob;
        public DownloadObject download;

        //public static bool operator == (ModfileObject a, ModfileObject b) => Equals(a, b);
        //public static bool operator !=(ModfileObject a, ModfileObject b) => !Equals(a, b);

        //public override bool Equals(object other) => GetHashCode() == other.GetHashCode();

        //public bool Equals(ModfileObject other) =>
        //        (id, mod_id, date_added, virus_status, virus_positive,
        //        virustotal_hash, filesize, filehash, filename, version,
        //        changelog, metadata_blob)
        //        ==
        //        (other.id, other.mod_id, other.date_added, other.virus_status,
        //        other.virus_positive, other.virustotal_hash, other.filesize, other.filehash,
        //        other.filename, other.version, other.changelog, other.metadata_blob);

        //public override int GetHashCode() => 
        //        (id, mod_id, date_added, virus_status, virus_positive,
        //        virustotal_hash, filesize, filehash, filename, version,
        //        changelog, metadata_blob).GetHashCode();
    }
}
