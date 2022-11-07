using System;
using System.Collections.Generic;
using ModIO.Implementation.API;
using ModIO.Implementation.API.Requests;
using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation
{
    /// <summary>
    /// Used to convert raw objects received from web requests into curated objects for the user.
    /// such as converting a ModObject into a ModProfile.
    /// </summary>
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
        public static ModPage ConvertResponseSchemaToModPage(GetMods.ResponseSchema schema, SearchFilter filter)
        {
            ModPage page = new ModPage();
            if(schema == null)
            {
                return page;
            }
            
            page.totalSearchResultsFound = schema.result_total;

            List<ModProfile> mods = new List<ModProfile>();
            int offset = filter.pageSize * filter.pageIndex;
            int highestModIndex = offset + filter.pageSize;
            // Only return the range of mods the user asked for (because we always take a minimum
            // of 100 mods per request, but they may have only asked for 10. We cache the other 90)
            for(int i = offset; i < highestModIndex && i < schema.data.Length; i++)
            {
                mods.Add(ConvertModObjectToModProfile(schema.data[i]));
            }
            
            ModProfile[] profiles = schema.data == null
                                        ? Array.Empty<ModProfile>()
                                        : ConvertModObjectsToModProfile(schema.data);

            page.modProfiles = mods.ToArray();
            
            // Add this response into the cache
            ModPage pageForCache = new ModPage();
            pageForCache.totalSearchResultsFound = schema.result_total;
            pageForCache.modProfiles = profiles;
            ResponseCache.AddModsToCache(GetMods.URL_Unpaginated(filter), offset, pageForCache);
            
            return page;
        }

        // The schema is identical to GetMods but left in here in case it changes in the future
        public static ModPage ConvertResponseSchemaToModPage(PaginatingRequest<ModObject> schema, SearchFilter filter)
        {
            ModPage page = new ModPage();
            if(schema == null)
            {
                return page;
            }
            page.totalSearchResultsFound = schema.result_total;
            
            List<ModProfile> mods = new List<ModProfile>();
            int offset = filter.pageSize * filter.pageIndex;
            int highestModIndex = offset + filter.pageSize;
            // Only return the range of mods the user asked for (because we always take a minimum
            // of 100 mods per request, but they may have only asked for 10. We cache the other 90)
            for(int i = offset; i < highestModIndex && i < schema.data.Length; i++)
            {
                mods.Add(ConvertModObjectToModProfile(schema.data[i]));
            }
            
            // LEGACY (Response Cache makes this no longer needed)
            // ModProfile[] profiles = schema.data == null
            //                             ? Array.Empty<ModProfile>()
            //                             : ConvertModObjectsToModProfile(schema.data);

            page.modProfiles = mods.ToArray();

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
            profile.status = (ModStatus)modObject.status;
            profile.visible = modObject.visible == 1;
            profile.contentWarnings = (ContentWarnings)modObject.maturity_option;
            profile.description = modObject.description_plaintext ?? "";
            profile.creatorUsername = modObject.submitted_by.username ?? "";
            profile.metadata = modObject.metadata_blob;
            profile.archiveFileSize = modObject.modfile.id == ModProfileNullId ? 
                ModProfileUnsetFilesize : modObject.modfile.filesize;

            // set time dates
            profile.dateLive = GetUTCDateTime(modObject.date_live);
            profile.dateAdded = GetUTCDateTime(modObject.date_added);
            profile.dateUpdated = GetUTCDateTime(modObject.date_updated);

            // set tags
            List<string> tags = new List<string>();
            foreach(ModTagObject tag in modObject.tags)
            {
                tags.Add(tag.name);
            }
            profile.tags = tags.ToArray();

            // set metadata kvps
            if(modObject.metadata_kvp != null)
            {
                profile.metadataKeyValuePairs = new KeyValuePair<string, string>[modObject.metadata_kvp.Length];
                for(int i = 0; i < modObject.metadata_kvp.Length; i++)
                {
                    profile.metadataKeyValuePairs[i] = new KeyValuePair<string, string>(
                        modObject.metadata_kvp[i].metakey,
                        modObject.metadata_kvp[i].metavalue);
                }
            }
            
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
            user.portal_username = userObject.display_name_portal;
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
