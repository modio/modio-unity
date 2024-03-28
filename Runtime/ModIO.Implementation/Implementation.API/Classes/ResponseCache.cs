using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API
{
    /// <summary>
    /// Manages checking, adding to and removing Key Value Pairs into a basic cache. Whenever a
    /// successful WebRequest has been deserialized and translated for the front end user the result
    /// can be added to the cache. Each entry is removed after 15000 milliseconds.
    /// </summary>
    internal static class ResponseCache
    {
#region Private class / wrappers for caching
        [Serializable]
        class CachedPageSearch
        {
            public Dictionary<int, long> mods = new Dictionary<int, long>();
            public long resultCount;
        }

        [Serializable]
        class CachedModProfile
        {
            public ModProfile profile;
            public bool extendLifetime;
        }
#endregion // Private class/wrappers for caching

        // whether or not to display verbose logs from the cache
        public static bool
            logCacheMessages = true;
        public static long maxCacheSize = 0;
        // 10 MiB is the minimum cache size estimate
        const int minCacheSize = 10485760;
        // 1 GiB is the absolute maximum for the cache size
        const int absoluteCacheSizeLimit = 1073741924;
        // milliseconds (60,000 being 60 seconds)
        const int modLifetimeInCache = 60000;

        static double lastWalletUpdateTime = 0;

        /// <summary>
        /// stores md5 hashes generated after retrieving Terms of Use from the RESTAPI
        /// </summary>
        public static TermsHash termsHash;

        // I'm currently using the Binary formatter as it's the cleanest implementation and remains
        // maintainable. There is definite room for optimisation but i'll wait until we settle on
        // a decision.
        // One known caveat is that it isn't 100% accurate because it can write int64s for example
        // with less than 64 bytes (due to serialisation). At worst I think we could possibly see
        // up to 10% leeway on the estimate, but i think this is acceptable for the use case
        // TODO @Steve Need to bench the binary formatter and see what the overhead might be
        // TODO: Test speed - Test accuracy
        // TODO: Optimise if going forward with BF

        /// <summary>
        /// All of the mods that have been obtained from the RESTAPI sorted according to the url
        /// that was used to obtain them.
        /// </summary>
        static Dictionary<string, CachedPageSearch> modPages =
            new Dictionary<string, CachedPageSearch>();

        static Dictionary<long, CachedModProfile> mods = new Dictionary<long, CachedModProfile>();
        static Dictionary<string, CommentPage> commentObjectsCache = new Dictionary<string, CommentPage>();
        static Dictionary<long, ModDependencies[]> modsDependencies = new Dictionary<long, ModDependencies[]>();
        static Dictionary<long, Rating> currentUserRatings = new Dictionary<long, Rating>();
        static Dictionary<string, Entitlement> entitlementsCache = new Dictionary<string, Entitlement>();
        static Dictionary<long, MonetizationTeamAccount[]> modsMonetizationTeams = new Dictionary<long, MonetizationTeamAccount[]>();
        static bool currentRatingsCached = false;
        static WalletObject walletObject;

        /// <summary>
        /// the terms of use, cached for the entire session.
        /// </summary>
        static KeyValuePair<string, TermsOfUse>? termsOfUse;

        /// <summary>
        /// The game tags, cached for the entire session.
        /// </summary>
        static TagCategory[] gameTags;

        /// <summary>The token packs, cached for the entire session.</summary>
        static TokenPack[] tokenPacks;

        /// <summary>
        /// The authenticated user profile, cached for the entire session or until fetch updates.
        /// </summary>
        static UserProfile? currentUser;

        public static int CacheSize => mods.Count;

        #region Adding entries to Cache

        public static void AddModsToCache(string url, int offset, ModPage modPage)
        {
            if(modPage.modProfiles == null)
            {
                return;
            }
            if(logCacheMessages)
            {
                Logger.Log(LogLevel.Verbose, $"[CACHE] adding {modPage.modProfiles.Length} mods to cache with url: {url}\n offset({offset})");
            }
            if(!modPages.ContainsKey(url))
            {
                modPages.Add(url, new CachedPageSearch());
            }

            // check for cache size clearing
            EnsureCacheSize(modPage);

            modPages[url].resultCount = modPage.totalSearchResultsFound;

            List<ModId> modIdsToClearAfterLifeTimeCheck = new List<ModId>();

            for(int i = 0; i < modPage.modProfiles.Length; i++)
            {
                // this line is just for ease of reading
                ModProfile mod = modPage.modProfiles[i];

                int index = i + offset;
                if(!modPages[url].mods.ContainsKey(index))
                {
                    modPages[url].mods.Add(index, mod.id);
                }

                if(!mods.ContainsKey(mod.id))
                {
                    CachedModProfile cachedMod = new CachedModProfile { profile = mod };
                    mods.Add(mod.id, cachedMod);
                    modIdsToClearAfterLifeTimeCheck.Add(mod.id);
                }
                else
                {
                    mods[mod.id].profile = mod;
                    mods[mod.id].extendLifetime = true;
                }
            }

            ClearModsFromCacheAfterDelay(modIdsToClearAfterLifeTimeCheck);
        }


        public static void AddModCommentsToCache(string url, CommentPage commentPage)
        {
            if(commentObjectsCache.ContainsKey(url))
            {
                commentObjectsCache[url] = commentPage;
            }
            else
            {
                commentObjectsCache.Add(url, commentPage);
            }
        }

        public static void RemoveModCommentFromCache(long id)
        {
            List<string> urls = new List<string>();
            foreach(var kvp in commentObjectsCache)
            {
                foreach(var commentObject in kvp.Value.CommentObjects)
                {
                    if(commentObject.id == id)
                    {
                        urls.Add(kvp.Key);
                    }
                }
            }

            foreach(var url in urls)
            {
                commentObjectsCache.Remove(url);
            }
        }

        public static void AddModToCache(ModProfile mod)
        {
            if(mods.ContainsKey(mod.id))
            {
                mods[mod.id].profile = mod;

                // we dont want to start another thread to clear this entry because one already
                // exists, therefore tag it to extend the lifetime.
                mods[mod.id].extendLifetime = true;
            }
            else
            {
                CachedModProfile cachedMod = new CachedModProfile();
                cachedMod.profile = mod;
                cachedMod.extendLifetime = false;
                mods.Add(mod.id, cachedMod);

                ClearModFromCacheAfterDelay(mod.id);
            }
        }

        public static void AddUserToCache(UserProfile profile)
        {
            currentUser = profile;
            lastWalletUpdateTime = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
        }

        public static void AddTagsToCache(TagCategory[] tags)
        {
            gameTags = tags;
        }

        public static void AddTokenPacksToCache(TokenPack[] tokenPacks) => ResponseCache.tokenPacks = tokenPacks;

        /// <summary>
        /// This caches the terms of use for the entire session. We only cache the ToS for one
        /// platform at a time. It is cached as a KeyValuePair to ensure we dont return the
        /// incorrect terms when given a different url for another platform (unlikely).
        /// </summary>
        /// <param name="url">the URL used to obtain the ToS</param>
        /// <param name="terms">the terms of use returned from the specified url</param>
        /// <exception cref="NotImplementedException"></exception>
        public static void AddTermsToCache(string url, TermsOfUse terms)
        {
            termsHash = terms.hash;
            termsOfUse = new KeyValuePair<string, TermsOfUse>(url, terms);
        }

        public static void AddModDependenciesToCache(ModId modId, ModDependencies[] modDependencies)
        {
            if(modsDependencies.ContainsKey(modId))
                modsDependencies[modId] = modDependencies;
            else
                modsDependencies.Add(modId, modDependencies);
        }
        public static void AddModMonetizationTeamToCache(ModId modId, MonetizationTeamAccount[] modMonetizationTeamAccounts)
        {
            modsMonetizationTeams[modId] = modMonetizationTeamAccounts;
        }

        public static void AddCurrentUserRating(long modId, Rating rating)
        {
            if(rating.rating == 0)
                currentUserRatings.Remove(modId);
            else if(currentUserRatings.ContainsKey(modId))
                currentUserRatings[modId] = rating;
            else
                currentUserRatings.Add(modId, rating);
        }

        private static void AddEntitlement(string transactionId, Entitlement entitlement)
        {
            if(entitlementsCache.ContainsKey(transactionId))
                entitlementsCache[transactionId] = entitlement;
            else
                entitlementsCache.Add(transactionId, entitlement);
        }

        public static void UpdateWallet(WalletObject wo)
        {
            walletObject = wo;
        }

        public static void UpdateWallet(int balance)
        {
            if (walletObject != null)
                walletObject.balance = balance;
        }

        #endregion // Adding entries to Cache

#region Getting entries from Cache
        /// <summary>
        /// Attempts to get the mods from the cache and outs them as a ModPage.
        /// </summary>
        /// <param name="url">IMPORTANT: The url must not include pagination, ie offset= and limit=</param>
        /// <returns>true if the cache had all of the entries</returns>
        public static bool GetModsFromCache(string url, int offset, int limit, out ModPage modPage)
        {
            if(logCacheMessages)
            {
                Logger.Log(LogLevel.Verbose, $"[CACHE] checking cache for mods for url: {url}\n offset({offset} limit({limit}))");
            }

            // do we contain this URL in the cache
            if(modPages.ContainsKey(url))
            {
                List<ModProfile> modsRetrievedFromCache = new List<ModProfile>();

                for(int i = 0; i < limit; i++)
                {
                    int index = i + offset;
                    if(index >= modPages[url].resultCount)
                    {
                        // We've reached the limit of mods available for this url
                        // This is not a fail
                        break;
                    }

                    if(modPages[url].mods.TryGetValue(index, out long modId))
                    {
                        if(mods.TryGetValue(modId, out CachedModProfile mod))
                        {
                            modsRetrievedFromCache.Add(mod.profile);

                            // Continue to next iteration because this one succeeded
                            continue;
                        }
                    }
                    else if(logCacheMessages)
                    {
                        Logger.Log(LogLevel.Verbose, $"[CACHE] failed to get one of the mods. {modsRetrievedFromCache.Count} mods found thus far. Failed at {index}. Total mods {modPages[url].mods.Count}");
                    }

                    // failed to get one of the mods for this cache request
                    modPage = default;
                    return false;
                }

                // SUCCEEDED
                if(logCacheMessages)
                {
                    Logger.Log(LogLevel.Verbose, $"[CACHE] retrieved {modsRetrievedFromCache.Count} mods from cache for url: {url}\n offset({offset} limit({limit}))");
                }

                modPage = new ModPage();
                modPage.totalSearchResultsFound = modPages[url].resultCount;
                modPage.modProfiles = modsRetrievedFromCache.ToArray();
                return true;
            }

            // Failed
            modPage = default;
            return false;
        }

        public static bool GetModFromCache(ModId modId, out ModProfile modProfile)
        {
            if(mods.ContainsKey(modId))
            {
                if(logCacheMessages)
                {
                    Logger.Log(LogLevel.Verbose, "[CACHE] retrieved mod from cache");
                }
                modProfile = mods[modId].profile;
                return true;
            }

            modProfile = default;
            return false;
        }

        public static bool GetUserProfileFromCache(out UserProfile userProfile)
        {
            if(currentUser != null)
            {
                if(logCacheMessages)
                {
                    Logger.Log(LogLevel.Verbose, "[CACHE] retrieved user profile from cache");
                }
                userProfile = currentUser.Value;
                return true;
            }

            userProfile = default;
            return false;
        }

        public static bool GetTagsFromCache(out TagCategory[] tags)
        {
            if(gameTags != null)
            {
                if(logCacheMessages)
                {
                    Logger.Log(LogLevel.Verbose, "[CACHE] retrieved game tags from cache");
                }
                tags = gameTags;
                return true;
            }

            tags = null;
            return false;
        }

        public static bool GetTokenPacksFromCache(out TokenPack[] tokenPacks)
        {
            if (ResponseCache.tokenPacks != null)
            {
                if(logCacheMessages)
                    Logger.Log(LogLevel.Verbose, "[CACHE] retrieved token packs from cache");

                tokenPacks = ResponseCache.tokenPacks;
                return true;
            }

            tokenPacks = null;
            return false;
        }

        public static bool GetModCommentsFromCache(string url, out CommentPage commentObjs)
        {
            if(commentObjectsCache.ContainsKey(url))
            {
                if(logCacheMessages)
                {
                    Logger.Log(LogLevel.Verbose, "[CACHE] retrieved mod comments from cache");
                }
                commentObjs = commentObjectsCache[url];
                return true;
            }

            commentObjs = new CommentPage();

            return false;
        }

        public static async Task<ResultAnd<byte[]>> GetImageFromCache(string url)
        {
            ResultAnd<byte[]> result = new ResultAnd<byte[]>();
            ResultAnd<byte[]> resultIO = await DataStorage.TryRetrieveImageBytes(url);

            result.result = resultIO.result;

            if(resultIO.result.Succeeded())
            {
                if(logCacheMessages)
                {
                    Logger.Log(LogLevel.Verbose,
                        "[CACHE] retrieved texture from temp folder cache");
                }
                result.value = resultIO.value;
            }

            return result;
        }

        public static async Task<ResultAnd<byte[]>> GetImageFromCache(
            DownloadReference downloadReference)
            => await GetImageFromCache(downloadReference.url);

        public static bool GetTermsFromCache(string url, out TermsOfUse terms)
        {
            if(termsOfUse != null)
            {
                if(termsOfUse.Value.Key == url)
                {
                    if(logCacheMessages)
                    {
                        Logger.Log(LogLevel.Verbose, "[CACHE] retrieved terms of use from cache");
                    }
                    terms = termsOfUse.Value.Value;
                    return true;
                }
            }

            terms = default;
            return false;
        }

        public static bool GetModDependenciesCache(ModId modId, out ModDependencies[] modDependencies)
        {
            if(modsDependencies.ContainsKey(modId))
            {
                if(logCacheMessages)
                {
                    Logger.Log(LogLevel.Verbose, "[CACHE] retrieved mod dependency from cache");
                }

                modDependencies = modsDependencies[modId];
                return true;
            }

            modDependencies = default;
            return false;
        }

        public static bool GetCurrentUserRatingsCache(out Rating[] ratings)
        {
            ratings = currentUserRatings.Values.ToArray();
            if(currentUserRatings == null)
            {
                return false;
            }

            if(logCacheMessages)
            {
                Logger.Log(LogLevel.Verbose, "[CACHE] retrieved mod rating from cache");
            }
            return true;

        }

        public static bool GetCurrentUserRatingFromCache(ModId modId, out ModRating modRating)
        {
            if(HaveRatingsBeenCachedThisSession())
            {
                if(currentUserRatings.TryGetValue(modId, out Rating rating))
                {
                    modRating = rating.rating;
                    return true;
                }
                modRating = ModRating.None;
                return true;
            }

            modRating = ModRating.None;
            return false;
        }

        public static bool HaveRatingsBeenCachedThisSession() => currentRatingsCached;

        public static bool GetWalletFromCache(out Wallet wo)
        {
            if(walletObject != null && DateTime.UtcNow.TimeOfDay.TotalMilliseconds - lastWalletUpdateTime >= modLifetimeInCache)
            {
                wo = ResponseTranslator.ConvertWalletObjectToWallet(walletObject);
                return true;
            }

            wo = default;
            return false;
        }

        public static bool GetModMonetizationTeamCache(ModId modId, out MonetizationTeamAccount[] teamAccounts)
        {
            if(modsMonetizationTeams.TryGetValue(modId, out teamAccounts))
            {
                if(logCacheMessages)
                {
                    Logger.Log(LogLevel.Verbose, "[CACHE] retrieved mod monetization team from cache");
                }
                return true;
            }

            return false;
        }

#endregion // Getting entries from Cache
#region Clearing Cache entries
        public static void ReplaceCurrentUserRatings(Rating[] ratings)
        {
            currentRatingsCached = true;
            currentUserRatings.Clear();
            foreach(var rating in ratings)
            {
                AddCurrentUserRating(rating.modId, rating);
            }
        }

        public static void ReplaceEntitlements(Entitlement[] entitlements)
        {
            entitlementsCache.Clear();
            foreach(var e in entitlements)
            {
                AddEntitlement(e.transactionId, e);
            }
        }

        internal static void ClearModFromCache(ModId modId) => mods.Remove(modId);

        static async void ClearModFromCacheAfterDelay(ModId modId)
        {
            do {
                if(mods.TryGetValue(modId, out CachedModProfile mod))
                {
                    mod.extendLifetime = false;
                }
                await Task.Delay(modLifetimeInCache); // 60 second cache
            } while(mods.ContainsKey(modId) && mods[modId].extendLifetime);

            if(mods.ContainsKey(modId))
            {
                mods.Remove(modId);
            }
        }

        static async void ClearModsFromCacheAfterDelay(List<ModId> modIds)
        {
            // Use this list to mark modIds that need to be cleared
            List<ModId> modIdsToClear = new List<ModId>();

            do {
                modIdsToClear.Clear();

                // Reset lifetime extension because we are about to wait a full lifetime cycle
                foreach(ModId modId in modIds)
                {
                    if(mods.TryGetValue(modId, out CachedModProfile mod))
                    {
                        mod.extendLifetime = false;
                    }
                }

                // wait for 60 seconds
                await Task.Delay(modLifetimeInCache);

                // check if any of the mods need to be cleared from the cache
                foreach(ModId modId in modIds)
                {
                    if(mods.TryGetValue(modId, out CachedModProfile mod))
                    {
                        if(mod.extendLifetime)
                        {
                            continue;
                        }
                    }

                    // mark the modId for removal
                    modIdsToClear.Add(modId);
                }

                // Check the modIds marked for removal and clear them from cache now
                foreach(ModId modId in modIdsToClear)
                {
                    modIds.Remove(modId);

                    if(mods.ContainsKey(modId))
                    {
                        mods.Remove(modId);
                    }
                }
            } while(modIds.Count > 0);
        }

        /// <summary>
        /// If the user has logged out we need to clear the cache for the user data.
        /// </summary>
        public static void ClearUserFromCache()
        {
            currentUser = null;
        }

        public static void ClearModMonetizationTeamFromCache(ModId modId)
        {
            modsMonetizationTeams.Remove(modId);
        }

        /// <summary>
        /// Clears the entire cache, used when performing a shutdown operation.
        /// </summary>
        public static void ClearCache()
        {
            modPages?.Clear();
            mods?.Clear();
            termsHash = default;
            termsOfUse = null;
            gameTags = null;
            commentObjectsCache.Clear();
            modsDependencies?.Clear();
            modsMonetizationTeams.Clear();
            currentUserRatings?.Clear();
            currentRatingsCached = false;
            walletObject = null;
            ClearUserFromCache();
        }
#endregion // Clearing Cache entries

#region Cache Size Checking

        /// <summary>
        /// Checks the size of the specified object against the cache to see if we need to make
        /// room in order to not exceed the max limit of the cache. If so, it forces the cache to
        /// clear the oldest entries (TODO @Steve check it is in fact clearing oldest, it's using
        /// a hash table, so that may not be the case)
        /// </summary>
        /// <param name="obj"></param>
        static void EnsureCacheSize(object obj)
        {
            // get the correct max size we should be checking for
            long maxSize = maxCacheSize < minCacheSize ? minCacheSize : maxCacheSize;
            maxSize = maxSize > absoluteCacheSizeLimit ? absoluteCacheSizeLimit : maxCacheSize;

            // Get the byte size estimates for the object and the current cache
            long cacheSize = GetCacheSizeEstimate();
            long newEntrySize = GetByteSizeForObject(obj);

            // if the size exceeds max size, make some room in the cache
            if(cacheSize + newEntrySize > maxSize)
            {
                ForceClearCache(cacheSize + newEntrySize - maxSize);
            }
        }

        /// <summary>
        /// continues to iterate over the mods cache clearing one at a time until the total number
        /// of bytes cleared exceeds the specified amount.
        /// </summary>
        /// <param name="numberOfBytesToClear">The number of bytes to try and clear from the cache</param>
        static void ForceClearCache(long numberOfBytesToClear)
        {
            if(mods == null)
            {
                return;
            }

            using(var enumerator = mods.GetEnumerator())
            {
                List<long> modsToRemove = new List<long>();
                long clearablebytes = 0;
                while(clearablebytes < numberOfBytesToClear && enumerator.MoveNext())
                {
                    // get the size of this mod
                    clearablebytes += GetByteSizeForObject(enumerator.Current.Value);
                    modsToRemove.Add(enumerator.Current.Key);
                }

                foreach(long modId in modsToRemove) { mods.Remove(modId); }
            }
        }

        static long GetCacheSizeEstimate()
        {
            long totalSize = GetModsByteSize();
            totalSize += GetByteSizeForObject(currentUser);
            totalSize += GetByteSizeForObject(gameTags);
            totalSize += GetByteSizeForObject(termsOfUse);
            if(logCacheMessages)
            {
                Logger.Log(LogLevel.Verbose, $"CACHE SIZE: {totalSize} bytes");
            }
            return totalSize;
        }

        static long GetModsByteSize()
        {
            long totalSize = 0;
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            if(mods != null)
            {
                // Get size of Mod Profiles
                using(CacheSizeStream stream = new CacheSizeStream())
                {
                    binaryFormatter.Serialize(stream, mods);
                    totalSize = stream.Length;
                }
            }

            if(modPages != null)
            {
                // Get size of ModPages
                using(CacheSizeStream stream = new CacheSizeStream())
                {
                    binaryFormatter.Serialize(stream, modPages);
                    totalSize += stream.Length;
                }
            }

            return totalSize;
        }

        static long GetByteSizeForObject(object obj)
        {
            if(obj == null)
            {
                return 0;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using(CacheSizeStream stream = new CacheSizeStream())
            {
                binaryFormatter.Serialize(stream, obj);
                return stream.Length;
            }
        }
#endregion // Cache Size Checking
    }
}
