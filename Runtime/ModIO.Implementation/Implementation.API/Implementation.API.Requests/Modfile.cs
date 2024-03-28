using System;

namespace ModIO.Implementation.API.Objects
{
    [Serializable]
    public struct Modfile
    {
        public long id;
        public long modId;
        public long dateAdded;
        public long dateScanned;
        public int virusStatus;
        public int virusPositive;
        public string virustotalHash;
        public long filesize;
        public string filehashMd5;
        public string filename;
        public string version;
        public string changelog;
        public string metadataBlob;
        public string downloadBinaryUrl;
        public long downloadDateExpires;
    }

}
