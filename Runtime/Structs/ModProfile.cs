using System;
using System.Collections.Generic;

namespace ModIO
{
    /// <summary>
    /// A struct representing all of the information available for a ModProfile.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetMod"/>
    /// <seealso cref="ModIOUnityAsync.GetMod"/>
    [Serializable]
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
        
        /// <summary>
        /// The meta data for this mod, not to be confused with the meta data of the specific version
        /// </summary>
        /// <seealso cref="InstalledMod"/>
        public string metadata;
        
        /// <summary>
        /// The most recent version of the mod that exists
        /// </summary>
        public string latestVersion;
        
        /// <summary>
        /// the change log for the most recent version of this mod
        /// </summary>
        public string latestChangelog;
        
        /// <summary>
        /// the date for when the most recent mod file was uploaded
        /// </summary>
        public DateTime latestDateFileAdded;

        /// <summary>
        /// the KVP meta data for this mod profile. Not to be confused with the meta data blob or
        /// the meta data for the installed version of the mod
        /// </summary>
        public KeyValuePair<string, string>[] metadataKeyValuePairs;
        
        public ModStats stats;
        public long archiveFileSize;
    }
}
