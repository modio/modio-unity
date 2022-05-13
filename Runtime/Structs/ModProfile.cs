using System;

namespace ModIO
{
    [System.Serializable]
    public struct ModProfile
    {
        public ModId id;
        public string[] tags;
        public ModStatus status;
        public bool visible;
        public string name;
        public string summary;
        public string description;
        public ContentWarnings contentWarnings;
        public DateTime dateAdded;
        public DateTime dateUpdated;
        public DateTime dateLive;
        public DownloadReference[] galleryImages_Original;
        public DownloadReference[] galleryImages_320x180;
        public DownloadReference[] galleryImages_640x360;
        public DownloadReference logoImage_320x180;
        public DownloadReference logoImage_640x360;
        public DownloadReference logoImage_1280x720;
        public DownloadReference logoImage_Original;
        public string creatorUsername;
        public DownloadReference creatorAvatar_50x50;
        public DownloadReference creatorAvatar_100x100;
        public DownloadReference creatorAvatar_Original;
        public string metadata;
        public ModStats stats;
        public long archiveFileSize;
    }
}
