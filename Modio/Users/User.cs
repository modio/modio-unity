using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Authentication;
using Modio.Caching;
using Modio.Collections;
using Modio.Errors;
using Modio.Extensions;
using Modio.FileIO;
using Modio.Mods;
using Modio.Monetization;
using Plugins.Modio.Modio.Ratings;

namespace Modio.Users
{
    public class User
    {
        /// <summary>
        /// An event that is invoked when the user changes.
        /// </summary>
        public static event Action<User> OnUserChanged;

        static event Action InternalOnUserChanged;
        
        /// <summary>
        /// An event that is invoked when the user is authenticated.
        /// If the user is already authenticated when a listener is added,
        /// it is immediately invoked.
        /// </summary>
        public static event Action OnUserAuthenticated
        {
            
            add
            {
                InternalOnUserChanged += value;
                
                if (Current is { IsAuthenticated: true, }) value?.Invoke();
            }
            
            remove => InternalOnUserChanged -= value;
        }

        public static User Current { get; private set; }
        
        public string LocalUserId { get; private set; }
        public long UserId => Profile.UserId;
        public bool IsInitialized { get; private set; }
        public bool HasAcceptedTermsOfUse { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public bool IsUpdating { get; private set; }
        public UserProfile Profile { get; private set; }
        public Wallet Wallet { get; private set; }
        public ModRepository ModRepository { get; private set; }
        public ModCollectionRepository ModCollectionRepository { get; private set; }

        readonly Authentication _authentication;
        bool _isWritingToDisk;
        bool _needsSavingToDisk;
        
        public static async Task InitializeNewUser()
        {
            ModioLog.Verbose?.Log($"Initializing New User");
            
            var user = new User();

            Current = user;

            Current.IsUpdating = true;

            Current.LocalUserId = await ModioServices.Resolve<IGetActiveUserIdentifier>().GetActiveUserIdentifier();
            
            (Error error, UserSaveObject userObject) = await ModioClient.DataStorage.ReadUserData(Current.LocalUserId);

            if (!error)
            {
                Current.ApplyDetailsFromSaveObject(userObject);
                Current.IsAuthenticated = true;
                Current.HasAcceptedTermsOfUse = true;
                InternalOnUserChanged?.Invoke();
                await Current.Sync();
                return;
            }

            Current.IsUpdating = false;
        }

        User()
        {
            _authentication = new Authentication();
            Wallet = new Wallet();
            ModRepository = new ModRepository();
            ModCollectionRepository = new ModCollectionRepository();
            
            Profile = new UserProfile();
            
            IsUpdating = false;
            IsAuthenticated = false;
            HasAcceptedTermsOfUse = false;

            IsInitialized = true;
        }

        void ApplyDetailsFromSaveObject(UserSaveObject userObject)
        {
            Profile.Username = userObject.Username;
            Profile.UserId = userObject.UserId;

            if (userObject.SubscribedMods != null)
                foreach (long subscribedMod in userObject.SubscribedMods)
                {
                    Mod mod = Mod.Get(subscribedMod);
                    mod.UpdateLocalSubscriptionStatus(true);
                }

            if (userObject.DisabledMods != null)
                foreach (long disabledMod in userObject.DisabledMods)
                {
                    Mod mod = Mod.Get(disabledMod);
                    mod.UpdateLocalEnabledStatus(false);
                }

            if (userObject.PurchasedMods != null)
                foreach (long purchasedMod in userObject.PurchasedMods)
                {
                    Mod mod = Mod.Get(purchasedMod);
                    mod.UpdateLocalPurchaseStatus(true);
                }
            
            if (userObject.FollowedCollections != null)
                foreach (long collection in userObject.FollowedCollections)
                {
                    ModCollection mod = ModCollection.Get(collection);
                    mod.UpdateLocalFollowStatus(true);
                }
            
            //For now just store the OAuthToken, we'll clarify if we're actually authenticated in Sync()
            _authentication.OAuthToken = userObject.AuthToken;
            
            OnUserChanged?.Invoke(this);
        }

        internal void OnAcceptedTermsOfUse()
        {
            HasAcceptedTermsOfUse = true;
        }

        public void OnAuthenticated(string oAuthToken)
        {
            _authentication.OAuthToken = oAuthToken;
            HasAcceptedTermsOfUse = true;
            IsAuthenticated = true;
            InternalOnUserChanged?.Invoke();

            // Using Task.Run() causes this to run in another thread, breaking sync completely as event subscribers
            // exist on main thread. By requiring it run synchronously we keep it on the Unity main thread.
            Sync().ForgetTaskSafely();
        }
        
        
        internal string GetAuthToken() => _authentication.OAuthToken;

        /// <summary>
        /// Syncs the user profile, subscriptions, purchases, wallet, ratings and entitlements with changes made from the Web Interface.
        /// </summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        public async Task<Error> Sync()
        {
            ModRepository.OnContentsChanged -= OnAnyModRepositoryChange;
            IsUpdating = true;
            
            Task<Error> profileTask = SyncProfile();
            Task<Error> subscriptionTask = SyncSubscriptions();
            Task<Error> purchaseTask = SyncPurchases();

            SyncRatings().ForgetTaskSafely();
            SyncWallet().ForgetTaskSafely();
            SyncEntitlements().ForgetTaskSafely();
            SyncCollections().ForgetTaskSafely();

            Error[] errors = await Task.WhenAll(profileTask, subscriptionTask, purchaseTask);

            IsUpdating = false;

            ModRepository.OnContentsChanged += OnAnyModRepositoryChange;
            
            // We already log the errors in their methods, so we just return the first one here
            if (errors.Any(error => error)) 
                return errors.First(error => error);
            
            // If there were errors, we probably don't want to save the broken data
            await SaveUserData();

            return Error.None;
        }

        void OnAnyModRepositoryChange() => SaveUserData().ForgetTaskSafely();

        /// <summary>
        /// Syncs the user profile with changes made on the WebInterface
        /// </summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        public async Task<Error> SyncProfile()
        {
            ModioLog.Verbose?.Log($"Syncing Profile {UserId}");
            
            (Error error, UserObject? userObject) = await ModioAPI.Me.GetAuthenticatedUser();
            
            if (error.Code == ErrorCode.EXPIRED_OR_REVOKED_ACCESS_TOKEN
                || error.Code == ErrorCode.HTTP_EXCEPTION)
            {
                IsUpdating = false;
                IsAuthenticated = false;
                _authentication.OAuthToken = null;

                await SaveUserData();
                
                return error;
            }

            if (!error)
                Profile.ApplyDetailsFromUserObject(userObject.Value);
            else if (!error.IsSilent)
                ModioLog.Error?.Log($"Error syncing User {UserId} profile details: {error}");
            
            // We set these here as the sync profile confirms if the access token's valid or not
            IsAuthenticated = true;
            HasAcceptedTermsOfUse = true;
            InternalOnUserChanged?.Invoke();
            OnUserChanged?.Invoke(this);
            
            ModioLog.Verbose?.Log($"Finished Syncing Profile {UserId} with result: {error}");

            return error;
        }

        /// <summary>
        /// Syncs the user subscriptions with changes made on the WebInterface
        /// </summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        public async Task<Error> SyncSubscriptions()
        {
            ModioLog.Verbose?.Log($"Syncing Subscriptions {UserId}");

            var filter = ModioAPI.Me.FilterGetUserSubscriptions().GameId(ModioClient.Settings.GameId);
    
            (Error error, List<ModObject> subObjects) = await CrawlAllPages(filter, ModioAPI.Me.GetUserSubscriptions);

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error syncing subscriptions for {UserId}: {error}");
                return error;
            }
            
            List<Mod> subscriptions = subObjects
                                      .Select(ModCache.GetMod)
                                      .ToList();

            // Get all previously, but not currently, subscribed mods and update their status
            var previouslySubscribed = new HashSet<Mod>(ModRepository.GetSubscribed());
            
            // Cull values that don't need to change
            foreach (Mod subscription in subscriptions)
            {
                previouslySubscribed.Remove(subscription);
                subscription.UpdateLocalSubscriptionStatus(true);
            }

            foreach (Mod mod in previouslySubscribed) 
                mod.UpdateLocalSubscriptionStatus(false);

            ModRepository.HasGotSubscriptions = true;
            OnUserChanged?.Invoke(this);
            
            ModioLog.Verbose?.Log($"Finished Syncing Subscriptions for {LocalUserId} with result: {error}");
            
            // Since this could add new mods that need to be installed, we wake up installation manager
            ModInstallationManagement.WakeUp();

            return Error.None;
        }
        
        /// <summary>
        /// Syncs the user purchases with changes made on the WebInterface
        /// </summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        public async Task<Error> SyncPurchases()
        {
            ModioLog.Verbose?.Log($"Syncing Purchases for {UserId}");

            var settings = ModioServices.Resolve<ModioSettings>();

            if (!settings.TryGetPlatformSettings(out MonetizationSettings _))
            {
                ModioLog.Message?.Log($"No {typeof(MonetizationSettings)} settings found, skipping SyncPurchases");
                return Error.None;
            }

            ModioAPI.Me.GetUserPurchasesFilter filter = ModioAPI.Me.FilterGetUserPurchases().GameId(settings.GameId);

            (Error error, List<ModObject> purchasedObjects) = await CrawlAllPages(filter, ModioAPI.Me.GetUserPurchases);

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error syncing purchases for {UserId}: {error}");
                return error;
            }

            List<Mod> purchases = purchasedObjects
                                  .Where(modObject => modObject.Visible != 0)
                                  .Select(ModCache.GetMod)
                                  .ToList();

            // Get all previously, but not currently, subscribed mods and update their status
            // Far less likely case than subscriptions, but mods can be refunded
            var previouslyPurchased = new HashSet<Mod>(ModRepository.GetPurchased());
            
            // Cull values that don't need to change
            foreach (Mod purchase in purchases)
            {
                previouslyPurchased.Remove(purchase);
                purchase.UpdateLocalPurchaseStatus(true);
            }

            foreach (Mod mod in previouslyPurchased) 
                mod.UpdateLocalPurchaseStatus(false);
            
            OnUserChanged?.Invoke(this);
            
            ModioLog.Verbose?.Log($"Finished Syncing Purchases for {LocalUserId} with result: {error}");

            // Since this could add new mods that need to be installed, we wake up installation manager
            ModInstallationManagement.WakeUp();

            return Error.None;
        }

        /// <summary>
        /// Syncs the user collections with changes made on the WebInterface
        /// </summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        public async Task<Error> SyncCollections()
        {
            ModioLog.Verbose?.Log($"Syncing Collections for {UserId}");

            var settings = ModioServices.Resolve<ModioSettings>();

            
            ModioAPI.Me.GetUserFollowedCollectionsFilter filter = ModioAPI.Me.FilterGetUserFollowedCollections().GameId(settings.GameId);

            (Error error, List<ModCollectionObject> collections) = await CrawlAllPages(filter, ModioAPI.Me.GetUserFollowedCollections);

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error syncing collections for {UserId}: {error}");
                return error;
            }

            List<ModCollection> subscribed = collections
                                             .Select(ModCollectionCache.Get)
                                             .ToList();

            // Get all previously, but not currently, subscribed mods and update their status
            // Far less likely case than subscriptions, but mods can be refunded
            var previouslyFollowed = new HashSet<ModCollection>(ModCollectionRepository.GetFollowed());
            
            // Cull values that don't need to change
            foreach (ModCollection collection in subscribed)
            {
                previouslyFollowed.Remove(collection);
                collection.UpdateLocalFollowStatus(true);
            }

            foreach (ModCollection collection in previouslyFollowed) 
                collection.UpdateLocalFollowStatus(false);
            
            OnUserChanged?.Invoke(this);
            
            ModioLog.Verbose?.Log($"Finished Syncing Collections for {UserId} with result: {error}");
            
            return Error.None;
        }
        
        /// <summary>
        /// Syncs the user entitlements with changes made on the WebInterface
        /// </summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        public async Task<Error> SyncEntitlements()
        {
            ModioLog.Verbose?.Log($"Syncing Entitlements {UserId}");

            var settings = ModioServices.Resolve<ModioSettings>();

            if (!settings.TryGetPlatformSettings(out MonetizationSettings _))
            {
                ModioLog.Message?.Log($"No {typeof(MonetizationSettings)} settings found, skipping SyncEntitlements");
                return Error.None;
            }
            
            if (!ModioServices.TryResolve(out IModioEntitlementService entitlementPlatform)) 
                return Error.None;

            Error error = await entitlementPlatform.SyncEntitlements();

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error syncing Entitlements for {UserId}: {error}");
                return error;
            }
            
            ModioLog.Verbose?.Log($"Finished Syncing Entitlements {LocalUserId} with result: {Error.None}");

            await SyncWallet();
            return Error.None;
        }

        /// <summary>
        /// Syncs the user wallet with changes made on the WebInterface
        /// </summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        public async Task<Error> SyncWallet()
        {
            ModioLog.Verbose?.Log($"Syncing Wallet {UserId}");

            var settings = ModioServices.Resolve<ModioSettings>();

            if (!settings.TryGetPlatformSettings(out MonetizationSettings monSettings))
            {
                ModioLog.Message?.Log($"No {typeof(MonetizationSettings)} settings found, skipping SyncWallet");
                return Error.None;
            }
            
            (Error error, WalletObject? walletObject) = await ModioAPI.Me.GetUserWallet(ModioClient.Settings.GameId);
            
            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error syncing Wallet for {UserId}: {error}");
                return error;
            }
            
            Wallet.ApplyDetailsFromWalletObject(walletObject.Value);
            OnUserChanged?.Invoke(this);
            
            ModioLog.Verbose?.Log($"Finished Syncing Wallet {LocalUserId} with result: {error}");

            return Error.None;
        }

        internal void ApplyWalletFromPurchase(PayObject payObject)
        {
            Wallet.UpdateBalance(payObject.Balance);
            OnUserChanged?.Invoke(this);
        }
        
        /// <summary>
        /// Syncs the user ratings with changes made on the WebInterface
        /// </summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        public async Task<Error> SyncRatings()
        {
            ModioLog.Verbose?.Log($"Syncing Ratings {UserId}");

            var settings = ModioServices.Resolve<ModioSettings>();

            var filter = ModioAPI.Me.FilterGetUserRatings();
            filter.GameId(settings.GameId);

            (Error error, List<RatingObject> ratedMods) = await CrawlAllPages(filter, ModioAPI.Me.GetUserRatings);

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error syncing Ratings for {UserId}: {error}");
                return error;
            }
            
            foreach (RatingObject ratingObject in ratedMods)
            {
                if (!ModCache.TryGetMod(ratingObject.ModId, out Mod mod))
                    continue;
                
                mod.SetCurrentUserRating((ModioRating)ratingObject.Rating);
            }
            
            ModioLog.Verbose?.Log($"Finished Syncing Ratings for {LocalUserId} with result: {error}");

            return Error.None;
        }

        /// <summary>Gets all users muted by the currently authenticated User from the API.</summary>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="IReadOnlyList{UserProfile}"/> results), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>result</c> is a readonly list of muted users <see cref="UserProfile"/>.</p>
        /// </returns>
        /// <remarks>All API requests and Mod requests will already filter out any entries by muted users.
        /// This will not cache any data. Please use sparingly.</remarks>
        public async Task<(Error error, IReadOnlyList<UserProfile> results)> GetMutedUsers()
        {
            (Error error, Pagination<UserObject[]>? userObjects) = await ModioAPI.Me.GetUsersMuted();

            if (error)
            {
                if (!error.IsSilent) 
                    ModioLog.Error?.Log($"Error getting muted users for user {Profile.Username}: {error}");
                return (error, Array.Empty<UserProfile>());
            }

            List<UserProfile> output = userObjects.Value.Data.Select(UserProfile.Get).ToList();

            return (Error.None, output);
        }

        /// <summary>Gets all mod creations by the User from the API.</summary>
        /// <param name="filterForGame">Optionally filter to only receive mods made for the current GameId</param>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="IReadOnlyList{Mod}"/> results), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>result</c> is a readonly list of <see cref="Mod"/>s created by this user.</p>
        /// </returns>
        public async Task<(Error error, IReadOnlyList<Mod> mods)> GetUserCreations(bool filterForGame = true)
        {
            ModioAPI.Me.GetUserModsFilter filter = ModioAPI.Me.FilterGetUserMods();
            
            var output = new List<Mod>();
            while (true)
            {
                (Error error, ModioPage<Mod> page) = await GetUserCreationsPaged(filter, filterForGame);
                
                if (error)
                {
                    if (!error.IsSilent) ModioLog.Error?.Log($"Error getting user creations for {UserId}: {error}");
                    return (error, Array.Empty<Mod>());
                }
                
                output.AddRange(page.Data);
                    
                if (page.HasMoreResults())
                    filter.PageIndex++;
                else
                    break;
            }
            
            return (Error.None, output);
        }

        /// <summary>
        /// Get all creations by the user. This does cache results
        /// </summary>
        public Task<(Error error, ModioPage<Mod> page)> GetUserCreationsPaged(
            ModioAPI.Mods.GetModsFilter modsFilter,
            bool filterForGame = true
        )
        {
            ModioAPI.Me.GetUserModsFilter filter = ModioAPI.Me.FilterGetUserMods(modsFilter.PageIndex, modsFilter.PageSize);

            foreach ((string key, object value) in modsFilter.Parameters)
                filter.Parameters[key] = value;

            return GetUserCreationsPaged(filter, filterForGame);
        }
        
        /// <summary>
        /// Get all creations by the user. This does cache results
        /// </summary>
        public async Task<(Error error, ModioPage<Mod> page)> GetUserCreationsPaged(ModioAPI.Me.GetUserModsFilter filter, bool filterForGame = true)
        {
            if(filterForGame)
                filter.GameId(ModioClient.Settings.GameId);
            //Don't show archived mods; those are only useful on the web interface
            filter.Status(3, Filtering.Not);

            string searchCacheKey = "me:yes,"+ ModCache.ConstructFilterKey(filter);
   
            if (ModCache.GetCachedModSearch(filter, searchCacheKey, out Mod[] cachedMods, out long resultTotal))
                return (Error.None, new ModioPage<Mod>(
                            cachedMods,
                            filter.PageSize,
                            filter.PageIndex,
                            resultTotal
                        ));

            
            (Error error, Pagination<ModObject[]>? pagination) = await ModioAPI.Me.GetUserMods(filter);
            
            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error getting user creations for {UserId}: {error}");
                return (error, null);
            }
            
            ModioPage<Mod> page = Mod.ConvertPaginationToModPage(pagination.Value, searchCacheKey);

            return (Error.None, page);
        }

        /// <summary>
        /// Gets all users following the currently authenticated User from the API.
        /// </summary>
        /// <returns>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="IReadOnlyList{UserProfile}"/> results), where:</returns>
        public async Task<(Error error, IReadOnlyList<UserProfile> results)> GetUsersFollowers()
        {
            ModioAPI.Me.GetUserFollowersFilter filter = ModioAPI.Me.FilterGetUserFollowers();
            
            (Error error, List<UserObject> userObjects) = await CrawlAllPages(filter, ModioAPI.Me.GetUserFollowers);
            if (error)
            {
                if (!error.IsSilent) 
                    ModioLog.Error?.Log($"Error getting followers of user {UserId}: {error}");
                return (error, Array.Empty<UserProfile>());
            }

            List<UserProfile> output = userObjects.Select(UserProfile.Get).ToList();

            return (Error.None, output);
        }
        
        /// <summary>
        /// Gets all users that the currently authenticated User is following from the API.
        /// </summary>
        /// <returns>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="IReadOnlyList{UserProfile}"/> results), where:</returns>
        public async Task<(Error error, IReadOnlyList<UserProfile> results)> GetUsersFollowing()
        {
            ModioAPI.Me.GetUsersFollowingFilter filter = ModioAPI.Me.FilterGetUsersFollowing();
            
            (Error error, List<UserObject> userObjects) = await CrawlAllPages(filter, ModioAPI.Me.GetUsersFollowing);
            if (error)
            {
                if (!error.IsSilent) 
                    ModioLog.Error?.Log($"Error getting followed users of user {UserId}: {error}");
                return (error, Array.Empty<UserProfile>());
            }

            List<UserProfile> output = userObjects.Select(UserProfile.Get).ToList();

            return (Error.None, output);
        }
        
        /// <summary>Removes the <see cref="User"/> and associated authentication and caches from this device.</summary>
        [ModioDebugMenu(ShowInBrowserMenu = false, ShowInSettingsMenu = true)]
        public static void DeleteUserData()
        {
            var dataStorage = ModioServices.Resolve<IModioDataStorage>();

            dataStorage.DeleteUserData(User.Current.LocalUserId).ForgetTaskSafely();
            
            LogOut();
        }

        /// <summary>Logs out the current <see cref="User"/> without deleting any associated data stored on this device.</summary>
        public static void LogOut()
        {
            ModioLog.Verbose?.Log($"Logging out {Current?.LocalUserId}");
            Current?.ModRepository.Dispose();
            Current = new User();
            OnUserChanged?.Invoke(Current);
        }

        /// <summary>
        /// Crawls all pages
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="method"></param>
        /// <typeparam name="F">The Filter type to use for this Crawl</typeparam>
        /// <typeparam name="T">The Type of object expected from the crawl</typeparam>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="List{T}"/> results), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>results</c> is a list of the expected type users.</p>
        /// </returns>
        static async Task<(Error error, List<T> results)> CrawlAllPages<F, T>(
            F filter, 
            Func<F, Task<(Error error, Pagination<T[]>?)>> method
        ) 
            where F : SearchFilter<F>
        {
            var output = new List<T>();
            
            while (true)
            {
                (Error error, Pagination<T[]>? result) 
                    = await method(filter);

                if (error)
                {
                    if (!error.IsSilent) 
                        ModioLog.Warning?.Log($"Error crawling pages for user {Current.UserId}: {error.GetMessage()}");
                    return (error, new List<T>());
                }

                output.AddRange(result.Value.Data);

                // Check for if we're on the last page, if not we increment the page number and iterate
                if (result.Value.ResultOffset + result.Value.ResultCount < result.Value.ResultTotal)
                    filter.PageIndex++;
                else
                    break;
            }

            return (Error.None, output);
        }
        
        async Task SaveUserData()
        {
            if (_isWritingToDisk)
            {
                _needsSavingToDisk = true;
                return;
            }
            _isWritingToDisk = true;
            _needsSavingToDisk = false;
            
            Error error = await ModioClient.DataStorage.WriteUserData(GetWritable());
            
            if (error) 
                ModioLog.Message?.Log($"Error writing user data to disk: {error.GetMessage()}");
            
            _isWritingToDisk = false;

            if (_needsSavingToDisk)
                await SaveUserData();
        }

        UserSaveObject GetWritable()
            => new UserSaveObject()
            {
                LocalUserId = LocalUserId,
                Username = Profile.Username,
                UserId = Profile.UserId,
                AuthToken = _authentication.OAuthToken,
                SubscribedMods = ModRepository.GetSubscribed().Select(mod => (long)mod.Id).ToList(),
                DisabledMods = ModRepository.GetDisabled().Select(mod => (long)mod.Id).ToList(),
                PurchasedMods = ModRepository.GetPurchased().Select(mod => (long)mod.Id).ToList(),
                FollowedCollections = ModCollectionRepository.GetFollowed().Select(collection => (long)collection.Id).ToList(),
            };
    }
    
    [Serializable]
    public class UserSaveObject
    {
        public string LocalUserId;
        public string Username;
        public long UserId;
        public string AuthToken;
        public long AuthExpiration;
        public List<long> SubscribedMods;
        public List<long> DisabledMods;
        public List<long> PurchasedMods;
        public List<long> FollowedCollections;
    }
}
