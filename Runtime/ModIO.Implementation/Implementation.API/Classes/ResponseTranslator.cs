using System;
using ModIO.Implementation.API.Requests;
using ModIO.Implementation.API.Objects;
using UnityEngine;

namespace ModIO.Implementation
{
    internal static class ResponseTranslator
    {
        const int ModProfileNullId = 0;
        const int ModProfileUnsetFilesize = -1;
        static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

        public static TermsOfUse ConvertTermsObjectToTermsOfUse(TermsObject termsObject)
        {
            TermsOfUse terms = new TermsOfUse();

            // Terms text
            terms.termsOfUse = termsObject.plaintext;

            // Links
            terms.links = new TermsOfUseLink[4];

            terms.links[0] = new TermsOfUseLink();
            terms.links[0].name = termsObject.links.website.text;
            terms.links[0].url = termsObject.links.website.url;
            terms.links[0].required = termsObject.links.website.required;

            terms.links[1] = new TermsOfUseLink();
            terms.links[1].name = termsObject.links.terms.text;
            terms.links[1].url = termsObject.links.terms.url;
            terms.links[1].required = termsObject.links.terms.required;

            terms.links[2] = new TermsOfUseLink();
            terms.links[2].name = termsObject.links.privacy.text;
            terms.links[2].url = termsObject.links.privacy.url;
            terms.links[2].required = termsObject.links.privacy.required;

            terms.links[3] = new TermsOfUseLink();
            terms.links[3].name = termsObject.links.manage.text;
            terms.links[3].url = termsObject.links.manage.url;
            terms.links[3].required = termsObject.links.manage.required;

            // File hash
            TermsHash hash = new TermsHash();
            hash.md5hash = IOUtil.GenerateMD5(terms.termsOfUse);

            return terms;
        }

        public static TagCategory[] ConvertGameTagOptionsObjectToTagCategories(
            GameTagOptionObject[] gameTags)
        {
            TagCategory[] categories = new TagCategory[gameTags.Length];

            for(int i = 0; i < categories.Length; i++)
            {
                categories[i] = new TagCategory();
                categories[i].name = gameTags[i].name ?? "";
                Tag[] tags = new Tag[gameTags[i].tags.Length];
                for(int ii = 0; ii < tags.Length; ii++)
                {
                    int total;
                    gameTags[i].tag_count_map.TryGetValue(gameTags[i].tags[ii], out total);
                    tags[ii].name = gameTags[i].tags[ii] ?? "";
                    tags[ii].totalUses = total;
                }

                categories[i].tags = tags;
                categories[i].multiSelect = gameTags[i].type == "checkboxes";
                categories[i].hidden = gameTags[i].hidden;
                categories[i].locked = gameTags[i].locked;
            }

            return categories;
        }
        public static ModPage ConvertResponseSchemaToModPage(GetMods.ResponseSchema schema)
        {
            ModPage page = new ModPage();
            if(schema == null)
            {
                return page;
            }
            
            page.totalSearchResultsFound = schema.result_total;

            ModProfile[] profiles = schema.data == null
                                        ? Array.Empty<ModProfile>()
                                        : ConvertModObjectsToModProfile(schema.data);

            page.modProfiles = profiles;

            return page;
        }

        // The schema is identical to GetMods but left in here in case it changes in the future
        public static ModPage ConvertResponseSchemaToModPage(PaginatingRequest<ModObject> schema)
        {
            ModPage page = new ModPage();
            if(schema == null)
            {
                return page;
            }
            page.totalSearchResultsFound = schema.result_total;

            ModProfile[] profiles = schema.data == null
                                        ? Array.Empty<ModProfile>()
                                        : ConvertModObjectsToModProfile(schema.data);

            page.modProfiles = profiles;

            return page;
        }

        public static ModProfile[] ConvertModObjectsToModProfile(ModObject[] modObjects)
        {
            ModProfile[] profiles = new ModProfile[modObjects.Length];

            for(int i = 0; i < profiles.Length; i++)
            {
                profiles[i] = ConvertModObjectToModProfile(modObjects[i]);
            }

            return profiles;
        }

        public static ModProfile ConvertModObjectToModProfile(ModObject modObject)
        {
            ModProfile profile = new ModProfile();

            profile.id = new ModId(modObject.id);
            profile.name = modObject.name ?? "";
            profile.summary = modObject.summary ?? "";
            profile.description = modObject.description_plaintext ?? "";
            profile.creatorUsername = modObject.submitted_by.username ?? "";
            profile.archiveFileSize = modObject.modfile.id == ModProfileNullId ? 
                ModProfileUnsetFilesize : modObject.modfile.filesize;

            // set time dates
            profile.dateLive = GetUTCDateTime(modObject.date_live);
            profile.dateAdded = GetUTCDateTime(modObject.date_added);
            profile.dateUpdated = GetUTCDateTime(modObject.date_updated);
            
            // Create DownloadReferences
            // Gallery
            if(modObject.media.images != null)
            {
                profile.galleryImages_320x180 =
                    new DownloadReference[modObject.media.images.Length];
                profile.galleryImages_640x360 =
                    new DownloadReference[modObject.media.images.Length];
                profile.galleryImages_Original =
                    new DownloadReference[modObject.media.images.Length];
                for(int i = 0; i < modObject.media.images.Length; i++)
                {
                    profile.galleryImages_320x180[i] = CreateDownloadReference(
                        modObject.media.images[i].filename, modObject.media.images[i].thumb_320x180,
                        profile.id);
                    profile.galleryImages_640x360[i] = CreateDownloadReference(
                        modObject.media.images[i].filename, modObject.media.images[i].thumb_320x180.Replace("320x180", "640x360"),
                        profile.id);
                    profile.galleryImages_Original[i] =
                        CreateDownloadReference(modObject.media.images[i].filename,
                                                modObject.media.images[i].original, profile.id);
                }
            }
            // Logo
            profile.logoImage_320x180 = CreateDownloadReference(
                modObject.logo.filename, modObject.logo.thumb_320x180, profile.id);
            profile.logoImage_640x360 = CreateDownloadReference(
                modObject.logo.filename, modObject.logo.thumb_640x360, profile.id);
            profile.logoImage_1280x720 = CreateDownloadReference(
                modObject.logo.filename, modObject.logo.thumb_1280x720, profile.id);
            profile.logoImage_Original = CreateDownloadReference(
                modObject.logo.filename, modObject.logo.original, profile.id);
            // Avatar
            profile.creatorAvatar_100x100 =
                CreateDownloadReference(modObject.submitted_by.avatar.filename,
                                        modObject.submitted_by.avatar.thumb_100x100, profile.id);
            profile.creatorAvatar_50x50 =
                CreateDownloadReference(modObject.submitted_by.avatar.filename,
                                        modObject.submitted_by.avatar.thumb_50x50, profile.id);
            profile.creatorAvatar_Original =
                CreateDownloadReference(modObject.submitted_by.avatar.filename,
                                        modObject.submitted_by.avatar.original, profile.id);

            // Mod Stats
            profile.stats = new ModStats() {
                modId = new ModId(modObject.stats.mod_id),
                downloadsToday = modObject.stats.downloads_today,
                downloadsTotal = modObject.stats.downloads_total,
                ratingsTotal = modObject.stats.ratings_total,
                ratingsNegative = modObject.stats.ratings_negative,
                ratingsPositive = modObject.stats.ratings_positive,
                ratingsDisplayText = modObject.stats.ratings_display_text,
                ratingsPercentagePositive = modObject.stats.ratings_percentage_positive,
                ratingsWeightedAggregate = modObject.stats.ratings_weighted_aggregate,
                popularityRankPosition = modObject.stats.popularity_rank_position,
                popularityRankTotalMods = modObject.stats.popularity_rank_total_mods,
                subscriberTotal = modObject.stats.subscribers_total
            };

            return profile;
        }

        static DownloadReference CreateDownloadReference(string filename, string url, ModId modId)
        {
            DownloadReference downloadReference = new DownloadReference();
            downloadReference.filename = filename;
            downloadReference.url = url;
            downloadReference.modId = modId;
            return downloadReference;
        }

        public static UserProfile ConvertUserObjectToUserProfile(UserObject userObject)
        {
            UserProfile user = new UserProfile();
            user.avatar_original = CreateDownloadReference(userObject.avatar.filename,
                                                           userObject.avatar.original, (ModId)0);
            user.avatar_50x50 = CreateDownloadReference(userObject.avatar.filename,
                                                        userObject.avatar.thumb_50x50, (ModId)0);
            user.avatar_100x100 = CreateDownloadReference(
                userObject.avatar.filename, userObject.avatar.thumb_100x100, (ModId)0);
            user.username = userObject.username;
            user.language = userObject.language;
            user.timezone = userObject.timezone;
            return user;
        }
        
#region Utility
        public static DateTime GetUTCDateTime(long serverTimeStamp)
        {
            DateTime dateTime = UnixEpoch.AddSeconds(serverTimeStamp);
            return dateTime;
        }
#endregion // Utility
    }
}
