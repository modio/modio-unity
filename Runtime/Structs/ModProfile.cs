using ModIO.Implementation.API.Objects;
using System;
using System.Collections.Generic;
#pragma warning disable CS0660, CS0661 // Don't want equality comparisons between two ModProfiles

namespace ModIO
{
    /// <summary>
    /// A struct representing all of the information available for a ModProfile.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetMod"/>
    /// <seealso cref="ModIOUnityAsync.GetMod"/>
    [Serializable]
    public readonly struct ModProfile
    {
        public readonly ModId id;
        public readonly string[] tags;
        public readonly ModStatus status;
        public readonly bool visible;
        public readonly string name;
        public readonly string summary;
        public readonly string description;
        public readonly string homePageUrl;
        public readonly string profilePageUrl;
        public readonly MaturityOptions maturityOptions;
        public readonly DateTime dateAdded;
        public readonly DateTime dateUpdated;
        public readonly DateTime dateLive;
        public readonly DownloadReference[] galleryImagesOriginal;
        public readonly DownloadReference[] galleryImages320x180;
        public readonly DownloadReference[] galleryImages640x360;
        public readonly DownloadReference logoImage320x180;
        public readonly DownloadReference logoImage640x360;
        public readonly DownloadReference logoImage1280x720;
        public readonly DownloadReference logoImageOriginal;
        public readonly UserProfile creator;
        public readonly DownloadReference creatorAvatar50x50;
        public readonly DownloadReference creatorAvatar100x100;
        public readonly DownloadReference creatorAvatarOriginal;
        public readonly string platformStatus;
        public readonly ModPlatform[] platforms;
        public readonly long gameId;
        public readonly int communityOptions;
        public readonly string nameId;
        public readonly Modfile modfile;

        // Marketplace
        public readonly RevenueType revenueType;
        public readonly int price;
        public readonly int tax;
        public readonly MonetizationOption MonetizationOption;

        //Scarcity
        public readonly int stock;

        /// <summary>
        /// The meta data for this mod, not to be confused with the meta data of the specific version
        /// </summary>
        /// <seealso cref="InstalledMod"/>
        public readonly string metadata;

        /// <summary>
        /// The most recent version of the mod that exists
        /// </summary>
        public readonly string latestVersion;

        /// <summary>
        /// the change log for the most recent version of this mod
        /// </summary>
        public readonly string latestChangelog;

        /// <summary>
        /// the date for when the most recent mod file was uploaded
        /// </summary>
        public readonly DateTime latestDateFileAdded;

        /// <summary>
        /// the KVP meta data for this mod profile. Not to be confused with the meta data blob or
        /// the meta data for the installed version of the mod
        /// </summary>
        public readonly KeyValuePair<string, string>[] metadataKeyValuePairs;

        public readonly ModStats stats;
        public readonly long archiveFileSize;

        public ModProfile(
            ModId id,
            string[] tags,
            ModStatus status,
            bool visible,
            string name,
            string summary,
            string description,
            string homePageUrl,
            string profilePageUrl,
            MaturityOptions maturityOptions,
            DateTime dateAdded,
            DateTime dateUpdated,
            DateTime dateLive,
            DownloadReference[] galleryImagesOriginal,
            DownloadReference[] galleryImages_320x180,
            DownloadReference[] galleryImages_640x360,
            DownloadReference logoImage_320x180,
            DownloadReference logoImage_640x360,
            DownloadReference logoImage_1280x720,
            DownloadReference logoImageOriginal,
            UserProfile creator,
            DownloadReference creatorAvatar_50x50,
            DownloadReference creatorAvatar_100x100,
            DownloadReference creatorAvatarOriginal,
            string metadata,
            string latestVersion,
            string latestChangelog,
            DateTime latestDateFileAdded,
            KeyValuePair<string, string>[] metadataKeyValuePairs,
            ModStats stats,
            long archiveFileSize,
            string platformStatus,
            ModPlatform[] platforms,
            RevenueType revenueType,
            int price,
            int tax,
            MonetizationOption monetizationOption,
            int stock,
            long gameId,
            int communityOptions,
            string nameId,
            Modfile modfile
        ) {

            this.id = id;
            this.tags = tags;
            this.status = status;
            this.visible = visible;
            this.name = name;
            this.summary = summary;
            this.description = description;
            this.homePageUrl = homePageUrl;
            this.profilePageUrl = profilePageUrl;
            this.maturityOptions = maturityOptions;
            this.dateAdded = dateAdded;
            this.dateUpdated = dateUpdated;
            this.dateLive = dateLive;
            this.galleryImagesOriginal = galleryImagesOriginal;
            this.galleryImages320x180 = galleryImages_320x180;
            this.galleryImages640x360 = galleryImages_640x360;
            this.logoImage320x180 = logoImage_320x180;
            this.logoImage640x360 = logoImage_640x360;
            this.logoImage1280x720 = logoImage_1280x720;
            this.logoImageOriginal = logoImageOriginal;
            this.creator = creator;
            this.creatorAvatar50x50 = creatorAvatar_50x50;
            this.creatorAvatar100x100 = creatorAvatar_100x100;
            this.creatorAvatarOriginal = creatorAvatarOriginal;
            this.metadata = metadata;
            this.latestVersion = latestVersion;
            this.latestChangelog = latestChangelog;
            this.latestDateFileAdded = latestDateFileAdded;
            this.metadataKeyValuePairs = metadataKeyValuePairs;
            this.stats = stats;
            this.archiveFileSize = archiveFileSize;
            this.platformStatus = platformStatus;
            this.platforms = platforms;
            this.revenueType = revenueType;
            this.price = price;
            this.tax = tax;
            this.MonetizationOption = monetizationOption;
            this.stock = stock;
            this.gameId = gameId;
            this.communityOptions = communityOptions;
            this.nameId = nameId;
            this.modfile = modfile;
        }

        public static bool operator ==(ModProfile left, ModId right) => left.id == right;
        public static bool operator !=(ModProfile left, ModId right) => left.id != right;
        public static bool operator ==(ModId left, ModProfile right) => right == left;
        public static bool operator !=(ModId left, ModProfile right) => right != left;
    }
}
