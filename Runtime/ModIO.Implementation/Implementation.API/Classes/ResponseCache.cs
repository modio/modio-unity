using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Transactions;
using UnityEngine;

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
        [System.Serializable]
        class CachedPageSearch
        {
            public Dictionary<int, long> mods = new Dictionary<int, long>();
            public long resultCount;
        }

        [System.Serializable]
        class CachedModProfile
        {
            public ModProfile profile;
            public bool extendLifetime;
        }
#endregion // Private class/wrappers for caching

        public static bool
            logCacheMessages; // whether or not to display verbose logs from the cache
        public static long maxCacheSize = 0;
        const int minCacheSize = 10485760; // 10 MiB is the minimum cache size estimate
        const int absoluteCacheSizeLimit =
            1073741924; // 1 GiB is the absolute maximum for the cache size
        const int modLifetimeInCache = 60000; // milliseconds

        /// <summary>
        /// stores md5 hashes generated after retrieving Terms of Use from the RESTAPI
        /// </summary>
        public static TermsHash termsHash = default;

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

        /// <summary>
        /// the terms of use, cached for the entire session.
        /// </summary>
        static KeyValuePair<string, TermsOfUse>? termsOfUse;

        /// <summary>
        /// The game tags, cached for the entire session.
        /// </summary>
        static TagCategory[] gameTags;

        /// <summary>
        /// The authenticated user profile, cached for the entire session or until fetch updates.
        /// </summary>
        static UserProfile? currentUser;

#region Adding entries to Cache

        public static void AddModsToCache(string url, int offset, ModPage modPage)
        {
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
                    modIdsToClearAfterLifeTimeCheck.Add(mod.id);
                }

                CachedModProfile cachedMod = new CachedModProfile();
                cachedMod.profile = mod;
                if(!mods.ContainsKey(mod.id))
                {
                    mods.Add(mod.id, cachedMod);
                }
                else
                {
                    mods[mod.id] = cachedMod;
                }
            }

            ClearModsFromCacheAfterDelay(modIdsToClearAfterLifeTimeCheck);
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
        }

        public static void AddTagsToCache(TagCategory[] tags)
        {
            gameTags = tags;
        }

        public static async Task AddTextureToCache(DownloadReference downloadReference, Texture2D texture)
        {
            // We can fire and forget, we will check the cache later if it worked with
            // GetTextureFromCache()
            await DataStorage.StoreImage(downloadReference, texture);

        }

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
#endregion // Adding entries to Cache

#region Getting entries from Cache
        /// <summary>
        /// Attempts to get the mods from the cache and outs them as a ModPage.
        /// </summary>
        /// <param name="url">IMPORTANT: The url must not include pagination, ie offset= and limit=</param>
        /// <returns>true if the cache had all of the entries</returns>
        public static bool GetModsFromCache(string url, int offset, int limit, out ModPage modPage)
        {
            // do we contain this URL in the cache
            if(modPages.ContainsKey(url))
            {
                List<ModProfile> modsRetrievedfromCache = new List<ModProfile>();

                for(int i = 0; i < limit; i++)
                {
                    int index = i + offset;
                    if(index > modPages[url].resultCount)
                    {
                        // We've reached the limit of mods available for this url
                        // This is not a fail
                        break;
                    }

                    if(modPages[url].mods.TryGetValue(index, out long modId))
                    {
                        if(mods.TryGetValue(modId, out CachedModProfile mod))
                        {
                            modsRetrievedfromCache.Add(mod.profile);

                            // Continue to next iteration because this one succeeded
                            continue;
                        }
                    }

                    goto Fail;
                }

                // SUCCEEDED
                if(logCacheMessages)
                {
                    Logger.Log(LogLevel.Verbose, "[CACHE] retrieved mods from cache");
                }

                modPage = new ModPage();
                modPage.totalSearchResultsFound = modPages[url].resultCount;
                modPage.modProfiles = modsRetrievedfromCache.ToArray();
                return true;
            }

        Fail:;
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

        public static async Task<ResultAnd<Texture2D>> GetTextureFromCache(
            DownloadReference downloadReference)
        {
            ResultAnd<Texture2D> result = new ResultAnd<Texture2D>();

            ResultAnd<Texture2D> resultIO = await DataStorage.TryRetrieveImage(downloadReference);

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
#endregion // Getting entries from Cache

#region Clearing Cache entries

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
        /// Clears the entire cache, used when performing a shutdown operation.
        /// </summary>
        public static void ClearCache()
        {
            modPages?.Clear();
            mods?.Clear();
            termsHash = default;
            termsOfUse = null;
            gameTags = null;
            currentUser = null;
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

            // Get the byte size estimates for th eobject and the current cache
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
