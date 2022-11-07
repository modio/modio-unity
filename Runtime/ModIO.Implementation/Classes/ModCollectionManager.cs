using System.Collections.Generic;
using System.Threading.Tasks;
using ModIO.Implementation.API;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.API.Requests;

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

#region Syncing User Fields
        static bool hasSyncedBefore;
        static long lastUserEventId;
        static long lastModEventId;
#endregion // Syncing User Fields

        public static async Task<Result> LoadRegistry()
        {
            // Reset syncing in case of re-init
            hasSyncedBefore = false;

            ResultAnd<ModCollectionRegistry> response = await DataStorage.LoadSystemRegistry();

            if(response.result.Succeeded())
            {
                Registry = response.value ?? new ModCollectionRegistry();
            }
            else
            {
                // Log error, failed to load registry
                Logger.Log(LogLevel.Error, $"Failed to load Registry [{response.result.code}]");
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
            lastUserEventId = 0;
            lastModEventId = 0;
            hasSyncedBefore = false;
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
        /// <param name="user">the modObject.io username of the user to sync</param>
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
            string url = GetAuthenticatedUser.URL();

            ResultAnd<UserObject> response =
                await RESTAPI.Request<UserObject>(url, GetAuthenticatedUser.Template);

            if(response.result.Succeeded())
            {
                await UserData.instance.SetUserObject(response.value);
            }
            else
            {
                return response.result;
            }
            
            // get the username we have in the registry
            long user = GetUserKey();
            
            //--------------------------------------------------------------------------------//
            //                               GET GAME TAGS                                    //
            //--------------------------------------------------------------------------------//
            // Get URL for tags request
            url = GetGameTags.URL();

            // Wait for unsub request
            ResultAnd<PaginatingRequest<GameTagOptionObject>> resultAnd =
                await RESTAPI.Request<PaginatingRequest<GameTagOptionObject>>(url,
                                                                              GetGameTags.Template);

            // If failed, cancel the entire update operation
            if(resultAnd.result.Succeeded())
            {
                await DataStorage.SaveGameTags(resultAnd.value.data);
            }
            else
            {
                return resultAnd.result;
            }

            //--------------------------------------------------------------------------------//
            //                SEND QUEUED UNSUBSCRIBES FROM BEING OFFLINE                     //
            //--------------------------------------------------------------------------------//
            foreach(ModId modId in Registry.existingUsers[user].unsubscribeQueue)
            {
                // Get URL for unsub request
                url = UnsubscribeFromMod.URL(modId);

                // Wait for unsub request
                result = await RESTAPI.Request(url, UnsubscribeFromMod.Template);

                // If failed, cancel the entire update operation
                if(!result.Succeeded())
                {
                    return result;
                }
            }

            //--------------------------------------------------------------------------------//
            //                         UPDATE MOD SUBSCRIPTIONS                               //
            //--------------------------------------------------------------------------------//
            result = !hasSyncedBefore ? await RunFirstSyncForTheSession(user)
                                      : await RunRepeatedSyncForTheSession(user);


            // Mark as true to change behaviour when re-checking (resets after new Init)
            hasSyncedBefore = true;

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
        static async Task<Result> RunFirstSyncForTheSession(long user)
        {
            Result result = ResultBuilder.Success;

            //--------------------------------------------------------------------------------//
            //                           GET SUBSCRIBED MODS                                  //
            //--------------------------------------------------------------------------------//

            string url = GetUserSubscriptions.URL();

            ResultAnd<ModObject[]> subscribedResultAnd =
                await RESTAPI.TryRequestAllResults<ModObject>(url, GetUserSubscriptions.Template);

            if(subscribedResultAnd.result.Succeeded())
            {
                // clear user's subscribed mods
                Registry.existingUsers[user].subscribedMods.Clear();

                foreach(ModObject mod in subscribedResultAnd.value)
                {
                    Registry.existingUsers[user].subscribedMods.Add(new ModId(mod.id));

                    UpdateModCollectionEntryFromModObject(mod);
                }
            }
            else
            {
                return subscribedResultAnd.result;
            }

            //--------------------------------------------------------------------------------//
            //                           GET LAST USER EVENT                                  //
            //--------------------------------------------------------------------------------//

            url = GetUserEvents.URL() + FilterUtil.LastEntryPagination();

            // Wait for request
            ResultAnd<GetUserEvents.ResponseSchema> userEventResultAnd =
                await RESTAPI.Request<GetUserEvents.ResponseSchema>(url, GetUserEvents.Template);

            if(userEventResultAnd.result.Succeeded())
            {
                // If exists
                if(userEventResultAnd.value.data?.Length > 0)
                {
                    // Cache event ID
                    lastUserEventId = userEventResultAnd.value.data[0].id;
                }
            }
            else
            {
                return userEventResultAnd.result;
            }

            //--------------------------------------------------------------------------------//
            //                            GET LAST MOD EVENT                                  //
            //--------------------------------------------------------------------------------//

            url = GetModEvents.URL() + FilterUtil.LastEntryPagination();

            // Wait for request
            ResultAnd<GetModEvents.ResponseSchema> modEventResultAnd =
                await RESTAPI.Request<GetModEvents.ResponseSchema>(url, GetModEvents.Template);

            if(modEventResultAnd.result.Succeeded())
            {
                // If exists
                if(modEventResultAnd.value.data?.Length > 0)
                {
                    // Cache event ID
                    lastModEventId = modEventResultAnd.value.data[0].id;
                }
            }
            else
            {
                return modEventResultAnd.result;
            }

            return result;
        }

        /// <summary>
        /// Goes through API requests that should be run after the first time a session has been
        /// synced for an authenticated user.
        /// </summary>
        /// <param name="user">username of the user</param>
        /// <returns>The Result of all the operations (returns at the first non-success result)</returns>
        static async Task<Result> RunRepeatedSyncForTheSession(long user)
        {
            Result result = ResultBuilder.Success;

            //--------------------------------------------------------------------------------//
            //                        GET LATEST USER EVENTS                                  //
            //--------------------------------------------------------------------------------//

            // get user events
            // Make sure it's ascending so we're iterating over a newer event each time
            string url = GetUserEvents.URL();
            url += $"&{Filtering.Ascending}id&id{Filtering.Min}{lastUserEventId}"
                + "&event_type-in=USER_SUBSCRIBE,USER_UNSUBSCRIBE";

            ResultAnd<UserEventObject[]> userResultAnd =
                await RESTAPI.TryRequestAllResults<UserEventObject>(url, GetUserEvents.Template);

            // TODO(@Steve): Consider replacing TryRequestAll with something like this:
            // ResultAnd<RequestPage> resultPage = await RESTAPI.Request<ModEventObject>(url,
            // GetModEvents.Template); if(resultPage.totalResults > resultPage.limit)
            // {
            //     // forget this, FetchAllUserSubscriptions and rebase the last event id
            // }

            if(userResultAnd.result.Succeeded())
            {
                // Cache the highest value we have
                lastUserEventId = userResultAnd.value?.Length > 0
                                      ? userResultAnd.value[userResultAnd.value.Length - 1].id
                                      : lastUserEventId;

                if(userResultAnd.value != null)
                {
                    foreach(UserEventObject userEvent in userResultAnd.value)
                    {
                        if(userEvent.event_type == "USER_SUBSCRIBE")
                        {
                            AddModToUserSubscriptions(new ModId(userEvent.mod_id));
                        }
                        else if(userEvent.event_type == "USER_UNSUBSCRIBE")
                        {
                            RemoveModFromUserSubscriptions(new ModId(userEvent.mod_id), false);
                        }
                    }
                }
            }
            else
            {
                return userResultAnd.result;
            }

            //--------------------------------------------------------------------------------//
            //                         GET LATEST MOD EVENTS                                  //
            //--------------------------------------------------------------------------------//

            // get modObject events
            url = GetModEvents.URL();
            url += $"&{Filtering.Ascending}id&id{Filtering.Min}{lastModEventId}"
                   + "&subscribed=true&event_type-in=MODFILE_CHANGED,MOD_EDITED";

            // create a placeholder pool for mods we will need in a GetMods request
            List<long> modsToGet = new List<long>();

            ResultAnd<ModEventObject[]> modEventResultAnd =
                await RESTAPI.TryRequestAllResults<ModEventObject>(url, GetModEvents.Template);

            // TODO(@Steve): Consider replacing TryRequestAll with something like this:
            // ResultAnd<RequestPage> resultPage = await RESTAPI.Request<ModEventObject>(url,
            // GetModEvents.Template); if(resultPage.totalResults > resultPage.limit)
            // {
            //     // forget this, FetchAllUserSubscriptions and rebase the last event id
            // }


            if(modEventResultAnd.result.Succeeded())
            {
                // Cache the highest value we have
                lastModEventId =
                    modEventResultAnd.value?.Length > 0
                        ? modEventResultAnd.value[modEventResultAnd.value.Length - 1].id
                        : lastModEventId;
                if(modEventResultAnd.value != null)
                {
                    foreach(ModEventObject modEvent in modEventResultAnd.value)
                    {
                        if(modEvent.event_type == "MODFILE_CHANGED"
                           || modEvent.event_type == "MOD_EDITED")
                        {
                            modsToGet.Add(modEvent.mod_id);
                        }
                    }
                }

                //--------------------------------------------------------------------------------//
                //                                 GET MODS                                       //
                //--------------------------------------------------------------------------------//
                if(modsToGet.Count > 0)
                {
                    url = GetMods.URL_Unpaginated();
                    foreach(long id in modsToGet) { url += $"&id={id}"; }

                    // TODO make sure our url isn't too long
                    // ...

                    ResultAnd<ModObject[]> modsResultAnd =
                        await RESTAPI.TryRequestAllResults<ModObject>(url, GetMods.Template);

                    if(modsResultAnd.result.Succeeded())
                    {
                        foreach(ModObject mod in modsResultAnd.value)
                        {
                            UpdateModCollectionEntryFromModObject(mod);
                        }
                    }
                    else
                    {
                        return modsResultAnd.result;
                    }
                }
            }
            else
            {
                return modEventResultAnd.result;
            }

            return result;
        }

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

        public static void UpdateModCollectionEntry(ModId modId, ModObject modObject)
        {
            AddModCollectionEntry(modId);

            Registry.mods[modId].modObject = modObject;

            // Check this in case of UserData being deleted
            if(DataStorage.TryGetInstallationDirectory(modId, modObject.modfile.id,
                                                       out string notbeingusedhere))
            {
                Registry.mods[modId].currentModfile = modObject.modfile;
            }

            SaveRegistry();
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

        public static void UpdateModCollectionEntryFromModObject(ModObject modObject,
                                                                 bool saveRegistry = true)
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

        /// <summary>
        /// Gets all mods that are installed regardless of whether or not the user is subscribed to
        /// them or not
        /// </summary>
        /// <returns></returns>
        public static InstalledMod[] GetInstalledMods(out Result result)
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
                    if(currentUser != 0 
                       && Registry.existingUsers.ContainsKey(currentUser)
                       && Registry.existingUsers[currentUser].subscribedMods.Contains(enumerator.Current.Key))
                    {
                        // dont include subscribed mods for the current user
                        continue;
                    }
                    
                    // check if current modfile is correct
                    if(DataStorage.TryGetInstallationDirectory(
                           enumerator.Current.Key.id, enumerator.Current.Value.currentModfile.id,
                           out string directory))
                    {
                        mods.Add(ConvertModCollectionEntryToInstalledMod(enumerator.Current.Value,
                                                                         directory));
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
                subscribedMods.Add(ConvertModCollectionEntryToSubscribedMod(entry));
            }

            result = ResultBuilder.Success;
            return subscribedMods.ToArray();
        }

        public static SubscribedMod ConvertModCollectionEntryToSubscribedMod(
            ModCollectionEntry entry)
        {
            SubscribedMod mod = new SubscribedMod();

            // generate ModProfile from ModObject
            mod.modProfile = ResponseTranslator.ConvertModObjectToModProfile(entry.modObject);

            // set the status for this subscribed mod
            mod.status = ModManagement.GetModCollectionEntrysSubscribedModStatus(entry);

            // assign directory field
            DataStorage.TryGetInstallationDirectory(entry.modObject.id, entry.modObject.modfile.id,
                                                    out mod.directory);

            return mod;
        }

        public static InstalledMod ConvertModCollectionEntryToInstalledMod(ModCollectionEntry entry,
                                                                           string directory)
        {
            InstalledMod mod = new InstalledMod();
            mod.modProfile = ResponseTranslator.ConvertModObjectToModProfile(entry.modObject);
            mod.updatePending = entry.currentModfile.id != entry.modObject.modfile.id;
            mod.directory = directory;
            mod.subscribedUsers = new List<long>();
            mod.metadata = entry.modObject.modfile.metadata_blob;

            foreach(long user in Registry.existingUsers.Keys)
            {
                if(Registry.existingUsers[user].subscribedMods.Contains((ModId)entry.modObject.id))
                {
                    mod.subscribedUsers.Add(user);
                }
            }

            return mod;
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
