using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModIO.Implementation.API;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.API.Requests;

#if UNITY_GAMECORE
using Unity.GameCore;
#endif

namespace ModIO.Implementation
{
    /// <summary>
    /// This class holds and manages all of the data relating to modObject collections inside of the
    /// ModCollectionRegistry class (ModCollectionManager.Registry). It also holds the subscriptions
    /// for each user that has been initialized. You can use GetSubscribedModsForUser(out Result) to
    /// get all of the subscribed mods for the current user.
    /// </summary>
    internal static class ModCollectionManager
    {
        public static ModCollectionRegistry Registry;

        public static async Task<Result> LoadRegistryAsync()
        {
            ResultAnd<ModCollectionRegistry> response = await DataStorage.LoadSystemRegistryAsync();

            if(response.result.Succeeded())
            {
                Registry = response.value ?? new ModCollectionRegistry();
            }
            else
            {
                // Log error, failed to load registry
                Logger.Log(LogLevel.Error, $"Failed to load Registry [{response.result.code}] - Creating a new one");
                Registry = new ModCollectionRegistry();

                return response.result;
            }

            return ResultBuilder.Success;
        }

        public static Result LoadRegistry()
        {
            ResultAnd<ModCollectionRegistry> response = DataStorage.LoadSystemRegistry();

            if(response.result.Succeeded())
            {
                Registry = response.value ?? new ModCollectionRegistry();
            }
            else
            {
                // Log error, failed to load registry
                Logger.Log(LogLevel.Error, $"Failed to load Registry [{response.result.code}] - Creating a new one");
                Registry = new ModCollectionRegistry();

                return response.result;
            }

            return ResultBuilder.Success;
        }

        public static async void SaveRegistry()
        {
            // Early out
            if(!IsRegistryLoaded())
            {
                Logger.Log(LogLevel.Error,
                    "ModCollectionManager was unable to save the Registry to disk because"
                    + " the registry hasn't been loaded yet");
                return;
            }

            Result result = await DataStorage.SaveSystemRegistry(Registry);

            if(!result.Succeeded())
            {
                Logger.Log(LogLevel.Error,
                           "ModCollectionManager was unable to save the Registry to disk"
                           + " because DataStorage.SaveSystemRegistry failed");
            }
        }

        public static void ClearRegistry()
        {
            Registry?.mods?.Clear();
            Registry?.existingUsers?.Clear();
            Registry = null;
        }

        public static void ClearUserData()
        {
            // Early out
            if(!IsRegistryLoaded() || !DoesUserExist())
            {
                return;
            }

            Registry.existingUsers.Remove(UserData.instance.userObject.id);

            SaveRegistry();
        }

        public static void AddUserToRegistry(UserObject user)
        {
            // Early out
            if(!IsRegistryLoaded())
            {
                return;
            }

            UserModCollectionData newUser = new UserModCollectionData();
            newUser.userId = user.id;

            if(!Registry.existingUsers.ContainsKey(user.id))
            {
                Registry.existingUsers.Add(user.id, newUser);
            }

            SaveRegistry();
        }

        /// <summary>
        /// Does a fetch for the user's subscriptions and syncs them with the registry.
        /// Also checks for updates for modfiles.
        /// </summary>
        /// <returns>true if the sync was successful</returns>
        public static async Task<Result> FetchUpdates()
        {
            // REVIEW @Jackson We should have a sweeping check of the installed directory to find
            // any installed mods that we may not know about in case of UserData being deleted/lost.
            // I've made a partial solution that checks if we have the most recent modfile of a
            // subscribed mod installed, but if a mod is outdated we will get artefacts.

            // Early out - make sure if user != null that we have a valid
            if(!IsRegistryLoaded())
            {
                return ResultBuilder.Create(ResultCode.Internal_RegistryNotInitialized);
            }

            //----[ Setup return ]----
            Result result = ResultBuilder.Success;

            //--------------------------------------------------------------------------------//
            //                           GET USER PROFILE DATA                                //
            //--------------------------------------------------------------------------------//
            //Leaving this in until the new API has finished implementation
            //string url = GetAuthenticatedUser.URL();
            //ResultAnd<UserObject> response =
            //    await RESTAPI.Request<UserObject>(url, GetAuthenticatedUser.Template);

            var profileResponse = await WebRequestManager.Request<UserObject>(API.Requests.GetAuthenticatedUser.Request());
            if(profileResponse.result.Succeeded())
            {
                UserData.instance.SetUserObject(profileResponse.value);
            }
            else
            {
                return profileResponse.result;
            }

            // get the username we have in the registry
            long user = GetUserKey();

            //--------------------------------------------------------------------------------//
            //                               GET GAME TAGS                                    //
            //--------------------------------------------------------------------------------//
            //Leaving this in until the new API has finished implementation
            // Get URL for tags request
            //url = GetGameTags.URL();
            //// Wait for unsub request
            //ResultAnd<PaginatedRequestResponse<GameTagOptionObject>> resultAnd =
            //    await RESTAPI.Request<PaginatedRequestResponse<GameTagOptionObject>>(url,
            //                                                                  GetGameTags.Template);

            var gameTagsResponse = await WebRequestManager.Request<API.Requests.GetGameTags.ResponseSchema>
                (API.Requests.GetGameTags.Request());

            // If failed, cancel the entire update operation
            if(gameTagsResponse.result.Succeeded())
            {
                // Put these in the Response Cache
                var tags = ResponseTranslator.ConvertGameTagOptionsObjectToTagCategories(gameTagsResponse.value.data);
                ResponseCache.AddTagsToCache(tags);
            }
            else
            {
                return gameTagsResponse.result;
            }

            //--------------------------------------------------------------------------------//
            //                SEND QUEUED UNSUBSCRIBES FROM BEING OFFLINE                     //
            //--------------------------------------------------------------------------------//
            foreach(ModId modId in Registry.existingUsers[user].unsubscribeQueue)
            {
                //Leaving this in until the new API has finished implementation
                //// Get URL for unsub request
                //url = UnsubscribeFromMod.URL(modId);
                //// Wait for unsub request
                //result = await RESTAPI.Request(url, UnsubscribeFromMod.Template);

                var unsubResponse = await WebRequestManager.Request(API.Requests.UnsubscribeFromMod.Request(modId));

                // If failed, cancel the entire update operation
                if(!unsubResponse.Succeeded())
                {
                    return result;
                }
            }

            //--------------------------------------------------------------------------------//
            //                            UPDATE CURRENT USER RATINGS                         //
            //--------------------------------------------------------------------------------//

            //Leaving this in until the new API has finished implementation
            //url = GetCurrentUserRatings.Url();
            //// Wait for request
            //ResultAnd<API2.Requests.GetCurrentUserRatings.ResponseSchema> ratingsResultAnd =
            //    await RESTAPI.Request<API2.Requests.GetCurrentUserRatings.ResponseSchema>(url, GetCurrentUserRatings.Template);

            string url = GetCurrentUserRatings.Request().Url;
            ResultAnd<RatingObject[]> ratingsResponse = await TryRequestAllResults<RatingObject>(url, GetCurrentUserRatings.Request);

            var ratings = ResponseTranslator.ConvertModRatingsObjectToRatings(ratingsResponse.value);

            // If failed, cancel the entire update operation
            if(ratingsResponse.result.Succeeded())
            {
                ResponseCache.ReplaceCurrentUserRatings(ratings);
            }
            else
            {
                // TODO dont cancel the entire function if we fail here
                return ratingsResponse.result;
            }

            //--------------------------------------------------------------------------------//
            //                         UPDATE MOD SUBSCRIPTIONS                               //
            //--------------------------------------------------------------------------------//
            result = await SyncUsersSubscriptions(user);

            if ((await ModIOUnityImplementation.IsMarketplaceEnabled(true)).Succeeded())
            {
                //--------------------------------------------------------------------------------//
                //                         UPDATE ENTITLEMENTS                                    //
                //--------------------------------------------------------------------------------//

                await ModIOUnityAsync.SyncEntitlements();


                //--------------------------------------------------------------------------------//
                //                         GET PURCHASES                                          //
                //--------------------------------------------------------------------------------//
                int pageSize = 100;
                int pageIndex = 0;
                bool continueFetching = true;

                while (continueFetching)
                {
                    SearchFilter filter = new SearchFilter(pageIndex, pageSize);
                    ResultAnd<ModPage> r = await ModIOUnityImplementation.GetUserPurchases(filter);
                    result = r.result;

                    if (r.result.Succeeded())
                    {
                        ResponseCache.AddModsToCache(API.Requests.GetUserPurchases.UnpaginatedURL(filter), 0, r.value);
                        AddModsToUserPurchases(r.value);
                        long totalResults = r.value.totalSearchResultsFound;
                        int resultsFetched = (pageIndex + 1) * pageSize;
                        if (resultsFetched > totalResults)
                        {
                            continueFetching = false;
                        }
                    }
                    else
                    {
                        continueFetching = false;
                    }

                    pageIndex++;
                }
            }
            //--------------------------------------------------------------------------------//
            //                         Finish Fetch                                           //
            //--------------------------------------------------------------------------------//

            ModManagement.WakeUp();
            Logger.Log(LogLevel.Message,
                       $"Finished syncing user[{user}:{UserData.instance.userObject.id}]");
            return result;
        }

        /// <summary>
        /// Goes through API requests that should only be run the first time we sync a session
        /// for an authenticated user.
        /// </summary>
        /// <param name="user">username of the user</param>
        /// <returns>The Result of all the operations (returns at the first non-success result)</returns>
        static async Task<Result> SyncUsersSubscriptions(long user)
        {
            Result result = ResultBuilder.Success;

            //--------------------------------------------------------------------------------//
            //                           GET SUBSCRIBED MODS                                  //
            //--------------------------------------------------------------------------------//
            //Leaving this in until the new API has finished implementation
            //string url = GetUserSubscriptions.URL();
            //ResultAnd<ModObject[]> subscribedResultAnd =
            //    await RESTAPI.TryRequestAllResults<ModObject>(url, GetUserSubscriptions.Template);
            WebRequestConfig config = API.Requests.GetUserSubscriptions.Request();
            ResultAnd<ModObject[]> subscribedResponse = await TryRequestAllResults<ModObject>(config.Url, ()=>config);

            if(subscribedResponse.result.Succeeded())
            {
                // clear user's subscribed mods
                Registry.existingUsers[user].subscribedMods.Clear();

                foreach(ModObject modObject in subscribedResponse.value)
                {
                    Registry.existingUsers[user].subscribedMods.Add(new ModId(modObject.id));
                    UpdateModCollectionEntryFromModObject(modObject);
                }
            }
            else
            {
                return subscribedResponse.result;
            }

            return result;
        }

        /// <summary>
        /// Does a recursive set of requests until all possible results are retrieved. Hard capped
        /// at 10 requests (1,000 results).
        /// </summary>
        /// <param name="url">The endpoint with relevant filters (But do not include pagination)</param>
        /// <param name="webrequestFactory"></param>
        /// <typeparam name="T">The data type of the page response schema (Make sure this is the
        /// correct API Object for the response schema relating to the endpoint being
        /// used)</typeparam>
        /// <returns>ResultAnd has the result of the entire operation and an array of all the
        /// retrieved results</returns>
        /// <remarks>Note this only works for GET requests</remarks>
        // TODO(@Steve): Add request limit
        // TODO(@Steve): Implement partial result?
        public static async Task<ResultAnd<T[]>> TryRequestAllResults<T>(
            string url, Func<WebRequestConfig> webrequestFactory)
        {
            const int maxNumberOfRequestsMade = 10;
            const int pageSize = 100;

            var response = new ResultAnd<T[]>();
            var collatedData = new List<T>();
            var numberOfRequestsMade = 0;

            long total = 0;

            do
            {
                var offset = pageSize * numberOfRequestsMade;

                //fetch, modify and run request
                var request = webrequestFactory();
                request.Url = $"{url}&{Filtering.Limit}{pageSize}&{Filtering.Offset}{offset}";
                var requestResult = await WebRequestManager.Request<PaginatedResponse<T>>(request);
                response.result = requestResult.result;

                if(!requestResult.result.Succeeded())
                    break;

                //set response
                total = requestResult.result.Succeeded() ? requestResult.value.result_total : 0;
                collatedData.AddRange(requestResult.value.data);

                //abort if our offset has reached more than the total
                if(pageSize * (numberOfRequestsMade + 1) >= total)
                    break;

                // check if we've reached max responses (10 pages or 1,000 results)
                numberOfRequestsMade++;
                if(numberOfRequestsMade >= maxNumberOfRequestsMade)
                {
                    Logger.Log(LogLevel.Warning,
                               "Recursive Paging method (TryRequestAllResults) has reached it's cap of 1,000 results. " +
                               "Ending now to avoid rate limiting.");
                    break;
                }
            }
            while(true);

            response.value = collatedData.ToArray();

            return response;
        }

        public static bool HasModCollectionEntry(ModId modId) => Registry.mods.ContainsKey(modId);

        public static void AddModCollectionEntry(ModId modId)
        {
            // Check an entry exists for this modObject, if not create one
            if(!Registry.mods.ContainsKey(modId))
            {
                ModCollectionEntry newEntry = new ModCollectionEntry();
                newEntry.modObject.id = modId;
                Registry.mods.Add(modId, newEntry);
            }
        }

        public static void UpdateModCollectionEntry(ModId modId, ModObject modObject, int priority = 0)
        {
            AddModCollectionEntry(modId);

            Registry.mods[modId].modObject = modObject;
            Registry.mods[modId].priority = priority;
            // Check this in case of UserData being deleted
            if(DataStorage.TryGetInstallationDirectory(modId, modObject.modfile.id,
                                                       out string notbeingusedhere))
            {
                Registry.mods[modId].currentModfile = modObject.modfile;
            }

            SaveRegistry();
        }

        private static void AddModsToUserPurchases(ModPage modPage)
        {
            foreach (ModProfile modProfile in modPage.modProfiles)
            {
                AddModToUserPurchases(modProfile.id);
            }
        }

        public static void AddModToUserPurchases(ModId modId, bool saveRegistry = true)
        {
            long user = GetUserKey();

            // Early out
            if(!IsRegistryLoaded() || !DoesUserExist(user))
            {
                return;
            }

            user = GetUserKey();

            if(!Registry.existingUsers[user].purchasedMods.Contains(modId))
            {
                Registry.existingUsers[user].purchasedMods.Add(modId);
            }

            // Check an entry exists for this modObject, if not create one
            AddModCollectionEntry(modId);

            if(saveRegistry)
            {
                SaveRegistry();
            }
        }

        public static void RemoveModFromUserPurchases(ModId modId, bool saveRegistry = true)
        {
            long user = GetUserKey();

            // Early out
            if(!IsRegistryLoaded() || !DoesUserExist(user))
            {
                Logger.Log(LogLevel.Warning, "registry not loaded");
                return;
            }

            user = GetUserKey();

            // Remove modId from user collection data
            if(Registry.existingUsers[user].purchasedMods.Contains(modId))
            {
                Registry.existingUsers[user].purchasedMods.Remove(modId);
            }

            if(saveRegistry)
            {
                SaveRegistry();
            }
        }

        public static void AddModToUserSubscriptions(ModId modId,
                                                     bool saveRegistry = true)
        {
            long user = GetUserKey();

            // Early out
            if(!IsRegistryLoaded() || !DoesUserExist(user))
            {
                return;
            }

            user = GetUserKey();

            if(!Registry.existingUsers[user].subscribedMods.Contains(modId))
            {
                Registry.existingUsers[user].subscribedMods.Add(modId);
            }

            // Check an entry exists for this modObject, if not create one
            AddModCollectionEntry(modId);

            if(saveRegistry)
            {
                SaveRegistry();
            }
        }

        public static void RemoveModFromUserSubscriptions(ModId modId, bool offline,
                                                          bool saveRegistry = true)
        {
            long user = GetUserKey();

            // Early out
            if(!IsRegistryLoaded() || !DoesUserExist(user))
            {
                Logger.Log(LogLevel.Warning, "registry not loaded");
                return;
            }

            user = GetUserKey();

            // Remove modId from user collection data
            if(Registry.existingUsers[user].subscribedMods.Contains(modId))
            {
                Registry.existingUsers[user].subscribedMods.Remove(modId);

                // Add unsubscribe to queue if offline
                if(offline)
                {
                    if(!Registry.existingUsers[user].unsubscribeQueue.Contains(modId))
                    {
                        Registry.existingUsers[user].unsubscribeQueue.Add(modId);
                    }
                }
            }

            if(saveRegistry)
            {
                SaveRegistry();
            }
        }

        public static void UpdateModCollectionEntryFromModObject(ModObject modObject, bool saveRegistry = true)
        {
            ModId modId = (ModId)modObject.id;

            // Add ModCollection entry if none exists
            if(!Registry.mods.ContainsKey(modId))
            {
                Registry.mods.Add(modId, new ModCollectionEntry());
            }

            // Update ModCollection
            Registry.mods[modId].modObject = modObject;

            // Check this in case of UserData being deleted
            if(DataStorage.TryGetInstallationDirectory(modId, modObject.modfile.id,
                                                       out string notbeingusedhere))
            {
                Registry.mods[modId].currentModfile = modObject.modfile;
            }

            if(saveRegistry)
            {
                SaveRegistry();
            }
        }

        public static bool EnableModForCurrentUser(ModId modId)
        {
            // early out
            if(!IsRegistryLoaded())
            {
                Logger.Log(LogLevel.Error, "Cannot enable mod for user. No registry has been loaded.");
                return false;
            }

            long currentUser = GetUserKey();

            if (Registry.existingUsers[currentUser].disabledMods.Contains(modId))
            {
                Registry.existingUsers[currentUser].disabledMods.Remove(modId);
            }

            Logger.Log(LogLevel.Verbose, $"Enabled Mod {((long)modId).ToString()}");
            return true;
        }

        public static bool DisableModForCurrentUser(ModId modId)
        {
            // early out
            if(!IsRegistryLoaded())
            {
                Logger.Log(LogLevel.Error, "Cannot enable mod for user. No registry has been loaded.");
                return false;
            }

            long currentUser = GetUserKey();

            if (!Registry.existingUsers[currentUser].disabledMods.Contains(modId))
            {
                Registry.existingUsers[currentUser].disabledMods.Add(modId);
            }

            Logger.Log(LogLevel.Verbose, $"Disabled Mod {((long)modId).ToString()}");
            return true;
        }

        /// <summary>
        /// Gets all mods that are purchased regardless of whether or not the user is subscribed to them or not
        /// </summary>
        /// <returns></returns>
        public static ModProfile[] GetPurchasedMods(out Result result)
        {
            // early out
            if(!IsRegistryLoaded())
            {
                result = ResultBuilder.Create(ResultCode.Internal_RegistryNotInitialized);
                return null;
            }

            List<ModProfile> mods = new List<ModProfile>();

            long currentUser = GetUserKey();

            using(var enumerator = Registry.existingUsers[currentUser].purchasedMods.GetEnumerator())
            {
                while(enumerator.MoveNext())
                {
                    if(Registry.mods.TryGetValue(enumerator.Current, out ModCollectionEntry entry))
                    {
                        mods.Add(ConvertModCollectionEntryToPurchasedMod(entry));
                    }
                }
            }

            result = ResultBuilder.Success;
            return mods.ToArray();
        }

        /// <summary>
        /// Gets all mods that are installed regardless of whether or not the user is subscribed to
        /// them or not
        /// </summary>
        /// <returns></returns>
        public static InstalledMod[] GetInstalledMods(out Result result, bool excludeSubscribedModsForCurrentUser)
        {
            // early out
            if(!IsRegistryLoaded())
            {
                result = ResultBuilder.Create(ResultCode.Internal_RegistryNotInitialized);
                return null;
            }

            List<InstalledMod> mods = new List<InstalledMod>();

            long currentUser = GetUserKey();

            using(var enumerator = Registry.mods.GetEnumerator())
            {
                while(enumerator.MoveNext())
                {
                    // Check if we are excluding the current authenticated user subscriptions or not
                    // If the current user is not authenticated we will obtain all installed mods
                    if(Registry.existingUsers.ContainsKey(currentUser)) // this checks if we're authenticated
                    {
                        if(excludeSubscribedModsForCurrentUser
                           && Registry.existingUsers[currentUser].subscribedMods.Contains(enumerator.Current.Key))
                        {
                            // dont include subscribed mods for the current user
                            continue;
                        }
                        if (!excludeSubscribedModsForCurrentUser
                                 && !Registry.existingUsers[currentUser].subscribedMods.Contains(enumerator.Current.Key))
                        {
                            // dont include non-subscribed mods for the current user
                            continue;
                        }
                    }

                    // check if current modfile is correct
                    if(DataStorage.TryGetInstallationDirectory(
                           enumerator.Current.Key.id, enumerator.Current.Value.currentModfile.id,
                           out string directory))
                    {
                        InstalledMod mod = ConvertModCollectionEntryToInstalledMod(enumerator.Current.Value, directory);
                        mod.enabled = Registry.existingUsers.ContainsKey(currentUser)
                                      && !Registry.existingUsers[currentUser].disabledMods.Contains(mod.modProfile.id);
                        mods.Add(mod);
                    }
                }
            }

            result = ResultBuilder.Success;
            return mods.ToArray();
        }

        /// <summary>
        /// Gets all of the ModCollectionEntry mods that a user has subscribed to.
        /// If "string user" is null it will default to the current initialized user.
        /// </summary>
        /// <param name="result">Will fail if the registry hasn't been initialized or the user doesn't exist</param>
        /// <param name="user">the user to check for in the registry (if null will use current user)</param>
        /// <returns>an array of the user's subscribed mods</returns>
        public static SubscribedMod[] GetSubscribedModsForUser(out Result result)
        {
            long user = GetUserKey();

            // Early out
            if(!IsRegistryLoaded() || !DoesUserExist())
            {
                result = ResultBuilder.Create(ResultCode.Internal_RegistryNotInitialized);
                return null;
            }

            user = GetUserKey();

            List<ModCollectionEntry> mods = new List<ModCollectionEntry>();

            // Use an enumerator because it's more performant
            using(var enumerator = Registry.existingUsers[user].subscribedMods.GetEnumerator())
            {
                while(enumerator.MoveNext())
                {
                    if(Registry.mods.TryGetValue(enumerator.Current, out ModCollectionEntry entry))
                    {
                        mods.Add(entry);
                    }
                }
            }

            // Convert ModCollections into user friendly SubscribedMods
            List<SubscribedMod> subscribedMods = new List<SubscribedMod>();

            foreach(ModCollectionEntry entry in mods)
            {
                SubscribedMod mod = ConvertModCollectionEntryToSubscribedMod(entry);
                mod.enabled = !Registry.existingUsers[user].disabledMods.Contains(mod.modProfile.id);
                subscribedMods.Add(mod);
            }

            result = ResultBuilder.Success;
            return subscribedMods.ToArray();
        }

        static SubscribedMod ConvertModCollectionEntryToSubscribedMod(ModCollectionEntry entry)
        {
            SubscribedMod mod = new SubscribedMod
            {
                modProfile = ResponseTranslator.ConvertModObjectToModProfile(entry.modObject),
                status = ModManagement.GetModCollectionEntrysSubscribedModStatus(entry),
            };

            // assign directory field
            DataStorage.TryGetInstallationDirectory(entry.modObject.id, entry.modObject.modfile.id,
                                                    out mod.directory);

            return mod;
        }

        static InstalledMod ConvertModCollectionEntryToInstalledMod(ModCollectionEntry entry, string directory)
        {
            InstalledMod mod = new InstalledMod
            {
                modProfile = ResponseTranslator.ConvertModObjectToModProfile(entry.modObject),
                updatePending = entry.currentModfile.id != entry.modObject.modfile.id,
                directory = directory,
                subscribedUsers = new List<long>(),
                metadata = entry.modObject.modfile.metadata_blob,
                version = entry.currentModfile.version,
                changeLog = entry.currentModfile.changelog,
                dateAdded = ResponseTranslator.GetUTCDateTime(entry.currentModfile.date_added),
            };

            foreach(long user in Registry.existingUsers.Keys)
            {
                if(Registry.existingUsers[user].subscribedMods.Contains((ModId)entry.modObject.id))
                {
                    mod.subscribedUsers.Add(user);
                }
            }

            return mod;
        }

        static ModProfile ConvertModCollectionEntryToPurchasedMod(ModCollectionEntry entry)
        {
            return ResponseTranslator.ConvertModObjectToModProfile(entry.modObject);
        }

        public static Result MarkModForUninstallIfNotSubscribedToCurrentSession(ModId modId)
        {
            // Early out
            if(!IsRegistryLoaded())
            {
                return ResultBuilder.Create(ResultCode.Internal_RegistryNotInitialized);
            }

            if(Registry.mods.TryGetValue(modId, out ModCollectionEntry mod))
            {
                mod.uninstallIfNotSubscribedToCurrentSession = true;
                return ResultBuilder.Success;
            }
            else
            {
                return ResultBuilder.Create(ResultCode.Unknown);
            }
        }

#region public Checks &Utility
        static bool IsRegistryLoaded()
        {
            // Early out
            if(Registry == null)
            {
                Logger.Log(LogLevel.Error, "The Registry hasn't been loaded yet. Make"
                                               + " sure you initialize the plugin before using this"
                                               + " method;");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the current user exists in the directory.
        /// </summary>
        /// <param name="user">if null, will use the current user</param>
        /// <returns>true if the user exists</returns>
        public static bool DoesUserExist(long user = 0)
        {
            if(user == 0)
            {
                if(UserData.instance?.userObject == null)
                {
                    Logger.Log(LogLevel.Error, "The current user data is null or hasn't been"
                                                   + " authenticated properly");
                    return false;
                }

                user = UserData.instance.userObject.id;

                if(user == 0)
                {
                    Logger.Log(LogLevel.Error, "The current user has not been authenticated "
                                                   + "properly (The UserObject id is not set).");
                    return false;
                }
            }

            if(!Registry.existingUsers.ContainsKey(user))
            {
                Logger.Log(LogLevel.Error,
                           $"The User does not exist in the current loaded Registry [{user}].");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Make sure to check DoesUserExist(user) before using this
        /// </summary>
        /// <returns>the user id of the current known UserObject we have stored (0 if none)</returns>
        public static long GetUserKey()
        {
            return UserData.instance == null ? 0 : UserData.instance.userObject.id;
        }
#endregion // public Checks & Utility
    }
}
