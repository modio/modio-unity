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
    }
}
