using ModIO.Implementation.API;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.API.Requests;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static TokenPack[] ConvertTokenPackObjectsToTokenPacks(IEnumerable<TokenPackObject> tokenPackObjects) => tokenPackObjects.GroupBy(tokenPackObject => tokenPackObject.token_pack_id).Select(tokenPack => new TokenPack(tokenPack)).ToArray();

        public static Wallet ConvertWalletObjectToWallet(WalletObject walletObject)
        {
            if (walletObject == null) return default;

            return new Wallet
            {
                currency = walletObject.currency,
                balance = walletObject.balance,
                type = walletObject.type
            };
        }

        public static TermsOfUse ConvertTermsObjectToTermsOfUse(TermsObject termsObject)
        {
            TermsOfUse terms = new TermsOfUse
            {
                termsOfUse = termsObject.plaintext,
                agreeText = termsObject.buttons.agree.text,
                disagreeText = termsObject.buttons.disagree.text,
                links = GetLinks(termsObject.links.website, termsObject.links.terms, termsObject.links.privacy, termsObject.links.manage),
                hash = new TermsHash
                {
                    md5hash = IOUtil.GenerateMD5(termsObject.plaintext),
                },
            };

            return terms;

            TermsOfUseLink[] GetLinks(params TermsLinkObject[] links)
            {
                return links.Select(link => new TermsOfUseLink
                {
                    name = link.text,
                    url = link.url,
                    required = link.required,
                }).ToArray();
            }
        }

        public static TagCategory[] ConvertGameTagOptionsObjectToTagCategories(
            GameTagOptionObject[] gameTags)
        {
            TagCategory[] categories = new TagCategory[gameTags.Length];

            for (int i = 0; i < categories.Length; i++)
            {
                categories[i] = new TagCategory();
                categories[i].name = gameTags[i].name ?? "";
                Tag[] tags = new Tag[gameTags[i].tags.Length];
                for (int ii = 0; ii < tags.Length; ii++)
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
        public static ModPage ConvertResponseSchemaToModPage(API.Requests.GetMods.ResponseSchema schema, SearchFilter filter)
        {
            ModPage page = new ModPage();
            if (schema == null)
            {
                return page;
            }

            page.totalSearchResultsFound = schema.result_total;

            List<ModProfile> mods = new List<ModProfile>();
            int offset = filter.pageSize * filter.pageIndex;
            // Only return the range of mods the user asked for (because we always take a minimum
            // of 100 mods per request, but they may have only asked for 10. We cache the other 90)
            for (int i = 0; i < filter.pageSize && i < schema.data.Length; i++)
            {
                mods.Add(ConvertModObjectToModProfile(schema.data[i]));
            }

            ModProfile[] profiles = schema.data == null
                                        ? Array.Empty<ModProfile>()
                                        : ConvertModObjectsToModProfiles(schema.data);

            page.modProfiles = mods.ToArray();

            // Add this response into the cache
            ModPage pageForCache = new ModPage();
            pageForCache.totalSearchResultsFound = schema.result_total;
            pageForCache.modProfiles = profiles;
            ResponseCache.AddModsToCache(GetMods.UnpaginatedURL(filter), offset, pageForCache);

            return page;
        }

        // The schema is identical to GetMods but left in here in case it changes in the future
        public static ModPage ConvertResponseSchemaToModPage(PaginatedResponse<ModObject> schema, SearchFilter filter)
        {
            ModPage page = new ModPage();
            if (schema == null)
            {
                return page;
            }
            page.totalSearchResultsFound = schema.result_total;

            List<ModProfile> mods = new List<ModProfile>();
            int offset = filter.pageSize * filter.pageIndex;
            int highestModIndex = offset + filter.pageSize;
            // Only return the range of mods the user asked for (because we always take a minimum
            // of 100 mods per request, but they may have only asked for 10. We cache the other 90)
            for (int i = offset; i < highestModIndex && i < schema.data.Length; i++)
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

        public static Rating[] ConvertModRatingsObjectToRatings(RatingObject[] ratingObjects)
        {
            Rating[] ratings = new Rating[ratingObjects.Length];
            int index = 0;
            foreach (var ratingObj in ratingObjects)
            {
                ratings[index++] = new Rating
                {
                    modId = new ModId(ratingObj.mod_id),
                    rating = (ModRating)ratingObj.rating,
                    dateAdded = GetUTCDateTime(ratingObj.date_added)
                };
            }

            return ratings;
        }

        public static ModDependencies[] ConvertModDependenciesObjectToModDependencies(ModDependenciesObject[] modDependenciesObjects)
        {
            ModDependencies[] modDependencies = new ModDependencies[modDependenciesObjects.Length];
            int index = 0;
            foreach (var modDepObj in modDependenciesObjects)
            {
                modDependencies[index] = new ModDependencies
                {
                    modId = new ModId(modDepObj.mod_id),
                    modName = modDepObj.mod_name,
                    dateAdded = GetUTCDateTime(modDepObj.date_added)
                };
                index++;
            }
            return modDependencies;
        }

        public static CommentPage ConvertModCommentObjectsToCommentPage(PaginatedResponse<ModCommentObject> commentObjects)
        {
            ModComment[] modComments = new ModComment[commentObjects.data.Length];

            for (int i = 0; i < commentObjects.data.Length; i++)
            {
                modComments[i] = ConvertModCommentObjectsToModComment(commentObjects.data[i]);
            }

            CommentPage page = new CommentPage
            {
                CommentObjects = modComments,
                totalSearchResultsFound = commentObjects.result_total
            };

            return page;
        }

        public static ModComment ConvertModCommentObjectsToModComment(ModCommentObject modCommentObjects)
        {
            return new ModComment
            {

                dateAdded = modCommentObjects.date_added,
                id = modCommentObjects.id,
                karma = modCommentObjects.karma,
                modId = (ModId)modCommentObjects.mod_id,
                resourceId = modCommentObjects.resource_id,
                submittedBy = modCommentObjects.submitted_by,
                threadPosition = modCommentObjects.thread_position,
                commentDetails = new CommentDetails(modCommentObjects.reply_id, modCommentObjects.content),
                userProfile = ConvertUserObjectToUserProfile(modCommentObjects.user)

            };

        }

        static ModProfile[] ConvertModObjectsToModProfiles(ModObject[] modObjects)
        {
            ModProfile[] profiles = new ModProfile[modObjects.Length];

            for (int i = 0; i < profiles.Length; i++)
            {
                profiles[i] = ConvertModObjectToModProfile(modObjects[i]);
            }

            return profiles;
        }

        public static Entitlement[] ConvertEntitlementObjectsToEntitlements(EntitlementObject[] entitlementObjects)
        {
            List<Entitlement> entitlements = new List<Entitlement>();

            foreach (var eo in entitlementObjects)
            {
                entitlements.Add(ConvertEntitlementObjectToEntitlement(eo));
            }

            return entitlements.ToArray();
        }

        public static CheckoutProcess ConvertCheckoutProcessObjectToCheckoutProcess(CheckoutProcessObject checkoutProcessObject)
        {
            return new CheckoutProcess()
            {
                transactionId = checkoutProcessObject.transaction_id,
                grossAmount = checkoutProcessObject.gross_amount,
                platformFee = checkoutProcessObject.platform_fee,
                tax = checkoutProcessObject.tax,
                purchaseDate = checkoutProcessObject.purchase_date,
                netAmount = checkoutProcessObject.net_amount,
                gatewayFee = checkoutProcessObject.gateway_fee,
                transactionType = checkoutProcessObject.transaction_type,
                meta = checkoutProcessObject.meta,
                walletType = checkoutProcessObject.wallet_type,
                balance = checkoutProcessObject.balance,
                deficit = checkoutProcessObject.deficit,
                paymentMethodId = checkoutProcessObject.payment_method_id,
                modProfile = ConvertModObjectToModProfile(checkoutProcessObject.mod),
            };
        }

        static Entitlement ConvertEntitlementObjectToEntitlement(EntitlementObject entitlementObject)
        {
            return new Entitlement()
            {
                transactionId = entitlementObject.transaction_id,
                transactionState = entitlementObject.transaction_state,
                entitlementConsumed = entitlementObject.entitlement_consumed,
                skuId = entitlementObject.sku_id
            };
        }

        public static ModProfile ConvertModObjectToModProfile(ModObject modObject)
        {
            if (modObject.id == 0)
            {
                // This is not a valid mod object
                Logger.Log(LogLevel.Error, "The method ConvertModObjectToModProfile(ModObject)"
                                           + " was given an invalid ModObject. This is an internal"
                                           + " error and should not happen.");
                return default;
            }

            ModId modId = new ModId(modObject.id);

            int galleryImagesCount = modObject.media.images?.Length ?? 0;
            DownloadReference[] galleryImages_320x180 = new DownloadReference[galleryImagesCount];
            DownloadReference[] galleryImages_640x360 = new DownloadReference[galleryImagesCount];
            DownloadReference[] galleryImages_Original = new DownloadReference[galleryImagesCount];
            for (int i = 0; i < galleryImagesCount; i++)
            {
                galleryImages_320x180[i] = CreateDownloadReference(
                    modObject.media.images[i].filename, modObject.media.images[i].thumb_320x180,
                    modId);
                galleryImages_640x360[i] = CreateDownloadReference(
                    modObject.media.images[i].filename, modObject.media.images[i].thumb_320x180.Replace("320x180", "640x360"),
                    modId);
                galleryImages_Original[i] =
                    CreateDownloadReference(modObject.media.images[i].filename,
                        modObject.media.images[i].original, modId);
            }

            KeyValuePair<string, string>[] metaDataKvp = modObject.metadata_kvp == null
                ? null
                : modObject.metadata_kvp
                    .Where(x => x.metakey != null)
                    .Select(kvp => new KeyValuePair<string, string>(kvp.metakey, kvp.metavalue)).ToArray();

            ModProfile profile = new ModProfile(
                modId,
                tags: modObject.tags == null ? Array.Empty<string>() : modObject.tags.Select(tag => tag.name).ToArray(),
                status: (ModStatus)modObject.status,
                visible: modObject.visible == 1,
                name: modObject.name ?? "",
                summary: modObject.summary ?? "",
                description: modObject.description_plaintext ?? "",
                homePageUrl: modObject.homepage_url,
                profilePageUrl: modObject.profile_url,
                maturityOptions: (MaturityOptions)modObject.maturity_option,
                dateAdded: GetUTCDateTime(modObject.date_added),
                dateUpdated: GetUTCDateTime(modObject.date_updated),
                dateLive: GetUTCDateTime(modObject.date_live),
                galleryImagesOriginal: galleryImages_Original,
                galleryImages_320x180: galleryImages_320x180,
                galleryImages_640x360: galleryImages_640x360,
                logoImage_320x180: CreateDownloadReference(modObject.logo.filename, modObject.logo.thumb_320x180, modId),
                logoImage_640x360: CreateDownloadReference(modObject.logo.filename, modObject.logo.thumb_640x360, modId),
                logoImage_1280x720: CreateDownloadReference(modObject.logo.filename, modObject.logo.thumb_1280x720, modId),
                logoImageOriginal: CreateDownloadReference(modObject.logo.filename, modObject.logo.original, modId),
                creator: ConvertUserObjectToUserProfile(modObject.submitted_by),
                creatorAvatar_50x50: CreateDownloadReference(modObject.submitted_by.avatar.filename, modObject.submitted_by.avatar.thumb_50x50, modId),
                creatorAvatar_100x100: CreateDownloadReference(modObject.submitted_by.avatar.filename, modObject.submitted_by.avatar.thumb_100x100, modId),
                creatorAvatarOriginal: CreateDownloadReference(modObject.submitted_by.avatar.filename, modObject.submitted_by.avatar.original, modId),
                metadata: modObject.metadata_blob,
                latestVersion: modObject.modfile.version,
                latestChangelog: modObject.modfile.changelog,
                latestDateFileAdded: GetUTCDateTime(modObject.modfile.date_added),
                metadataKeyValuePairs: metaDataKvp,
                stats: ConvertModStatsObjectToModStats(modObject.stats),
                archiveFileSize: modObject.modfile.id == ModProfileNullId ? ModProfileUnsetFilesize : modObject.modfile.filesize,
                platformStatus: modObject.platform_status,
                platforms: ConvertModPlatformsObjectsToModPlatforms(modObject.platforms),
                revenueType: modObject.revenue_type,
                price: modObject.price,
                tax: modObject.tax,
                monetizationOption: (MonetizationOption)modObject.monetisation_options,
                stock: modObject.stock,
                gameId: modObject.game_id,
                communityOptions: modObject.community_options,
                nameId:modObject.name_id,
                modfile: ConvertModfileObjectToModfile(modObject.modfile)
                );

            return profile;
        }

        private static ModPlatform[] ConvertModPlatformsObjectsToModPlatforms(ModPlatformsObject[] modPlatformsObjects)
        {
            ModPlatform[] modPlatforms = new ModPlatform[modPlatformsObjects.Length];
            for (int i = 0; i < modPlatformsObjects.Length; i++)
            {
                modPlatforms[i] = new ModPlatform()
                {
                    platform = modPlatformsObjects[i].platform,
                    modfileLive = modPlatformsObjects[i].modfile_live
                };
            }
            return modPlatforms;
        }

        private static Modfile ConvertModfileObjectToModfile(ModfileObject modfileObject)
        {
            return new Modfile()
            {
                id = modfileObject.id,
                modId = modfileObject.mod_id,
                dateAdded = modfileObject.date_added,
                dateScanned = modfileObject.date_scanned,
                virusStatus = modfileObject.virus_status,
                virusPositive = modfileObject.virus_positive,
                virustotalHash = modfileObject.virustotal_hash,
                filesize = modfileObject.filesize,
                filehashMd5 = modfileObject.filehash.md5,
                filename = modfileObject.filename,
                version = modfileObject.version,
                changelog = modfileObject.changelog,
                metadataBlob = modfileObject.metadata_blob,
                downloadBinaryUrl = modfileObject.download.binary_url,
                downloadDateExpires = modfileObject.download.date_expires,
            };
        }

        private static ModStats ConvertModStatsObjectToModStats(ModStatsObject modStatsObject)
        {
            return new ModStats()
            {
                modId = modStatsObject.mod_id,
                popularityRankPosition = modStatsObject.popularity_rank_position,
                popularityRankTotalMods = modStatsObject.popularity_rank_total_mods,
                downloadsToday = modStatsObject.downloads_today,
                downloadsTotal = modStatsObject.downloads_total,
                subscriberTotal = modStatsObject.subscribers_total,
                ratingsTotal = modStatsObject.ratings_total,
                ratingsPositive = modStatsObject.ratings_positive,
                ratingsNegative = modStatsObject.ratings_negative,
                ratingsPercentagePositive = modStatsObject.ratings_percentage_positive,
                ratingsWeightedAggregate = modStatsObject.ratings_weighted_aggregate,
                ratingsDisplayText = modStatsObject.ratings_display_text,
                dateExpires = modStatsObject.date_expires
            };
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
            UserProfile user = new UserProfile
            {
                avatar_original = CreateDownloadReference(userObject.avatar.filename,
                    userObject.avatar.original, (ModId)0),
                avatar_50x50 = CreateDownloadReference(userObject.avatar.filename,
                    userObject.avatar.thumb_50x50, (ModId)0),
                avatar_100x100 = CreateDownloadReference(
                    userObject.avatar.filename, userObject.avatar.thumb_100x100, (ModId)0),
                username = userObject.username,
                userId = userObject.id,
                portal_username = userObject.display_name_portal,
                language = userObject.language,
                timezone = userObject.timezone,
            };
            return user;
        }

        public static MonetizationTeamAccount[] ConvertGameMonetizationTeamObjectsToGameMonetizationTeams(MonetizationTeamAccountsObject[] monetizationTeamAccountsObjects)
        {
            MonetizationTeamAccount[] entitlements = new MonetizationTeamAccount[monetizationTeamAccountsObjects.Length];

            for (var i = 0; i < monetizationTeamAccountsObjects.Length; i++)
            {
                entitlements[i] = ConvertGameMonetizationTeamObjectToGameMonetizationTeam(monetizationTeamAccountsObjects[i]);
            }
            return entitlements;
        }

        public static MonetizationTeamAccount ConvertGameMonetizationTeamObjectToGameMonetizationTeam(MonetizationTeamAccountsObject team)
        {
            return new MonetizationTeamAccount
            {
                Id = team.id,
                NameId = team.name_id,
                Username = team.username,
                MonetizationStatus = team.monetization_status,
                MonetizationOptions = team.monetization_options,
                SplitPercentage = team.split,
            };
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
