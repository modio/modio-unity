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
        public ModioAPI.Portal AuthenticatedPortal { get; private set; }

        readonly Authentication _authentication;
        bool _needsSavingToDisk;
        List<UserProfile> _followed = new List<UserProfile>();
        Dictionary<ModioId, ModioRating> _modRatings = new Dictionary<ModioId, ModioRating>();
        Dictionary<ModioId, ModioRating> _collectionRatings = new Dictionary<ModioId, ModioRating>();
        
        List<CachedEntitlement> _cachedEntitlements;
        
        TaskCompletionSource<Error> _writeTcs;

        public static async Task InitializeNewUser()
        {
            ModioLog.Verbose?.Log($"Initializing New User");
            
            var user = new User();

            Current = user;

            Current.IsUpdating = true;

            Current.LocalUserId = await ModioServices.Resolve<IGetActiveUserIdentifier>().GetActiveUserIdentifier();

            if (string.IsNullOrEmpty(Current.LocalUserId))
            {
                Current.IsUpdating = false;
                return;
            }
            
            (Error error, UserSaveObject userObject) = await ModioClient.DataStorage.ReadUserData(Current.LocalUserId);

            if (!error)
            {
                Current.ApplyDetailsFromSaveObject(userObject);
                Current.IsAuthenticated = true;
                Current.HasAcceptedTermsOfUse = true;
                InternalOnUserChanged?.Invoke();
                error = await Current.Sync();

                if (error.Code is ErrorCode.USER_NOT_AUTHENTICATED or ErrorCode.USER_NO_ACCEPT_TERMS_OF_USE)
                    Current.IsAuthenticated = false;

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

        /// <summary>
        /// This method makes sure we wait for any in progress (and queued) write operations to complete before shutting
        /// down everything else. Helps protect against rare edge cases where a queued write can throw hands with the
        /// file system over a file handle it shouldn't have anymore.
        /// </summary>
        internal static async Task Shutdown()
        {
            if (Current?._writeTcs == null)
                return;

            await Current._writeTcs.Task;
        }

        void ApplyDetailsFromSaveObject(UserSaveObject userObject)
        {
            Profile.Username = userObject.Username;
            Profile.UserId = userObject.UserId;
            AuthenticatedPortal = (ModioAPI.Portal)userObject.UserPortal;

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

        public void OnAuthenticated(string oAuthToken, long dateExpires, bool sync = true)
        {
            ApplyAuthenticationAsync(oAuthToken, sync).ForgetTaskSafely();
        }

        public async Task ApplyAuthenticationAsync(string oAuthToken, bool sync)
        {
            IsUpdating = true;
            // At this point a local account had definitely been chosen, so we're best off rechecking if a user file
            // exists on system
            Current.LocalUserId = await ModioServices.Resolve<IGetActiveUserIdentifier>().GetActiveUserIdentifier();

            if (!string.IsNullOrEmpty(Current.LocalUserId))
            {
                (Error error, UserSaveObject userObject) = await ModioClient.DataStorage.ReadUserData(Current.LocalUserId);

                if (!error)
                    Current.ApplyDetailsFromSaveObject(userObject);
            }
            
            _authentication.OAuthToken = oAuthToken;
            HasAcceptedTermsOfUse = true;
            bool hasAuthenticated = IsAuthenticated;
            IsAuthenticated = true;
            AuthenticatedPortal = ModioAPI.CurrentPortal;

            await SaveUserData();
            
            InternalOnUserChanged?.Invoke();
            
            // if Sync only runs if the user has not authenticated before,
            // or we are forcing a sync
            // this is to avoid a re-authenticating user causing a sync
            // if a user is re-authenticating the endpoint triggering this
            // will sync the correct user data
            // ie; subscribing to a mod does not need to sync subscriptions
            // as the user has already been synced before, and only the subscription they're changing
            // needs to be updated
            
            if(sync || !hasAuthenticated)
                Sync().ForgetTaskSafely();
            else
                IsUpdating = false;
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
            
            ClearCachedUserCreations();
            
            Task<Error> profileTask = SyncProfile();
            Task<Error> subscriptionTask = SyncSubscriptions();
            Task<Error> purchaseTask = SyncPurchases();
            
            SyncModRatings().ForgetTaskSafely();
            SyncCollectionRatings().ForgetTaskSafely();
            SyncEntitlements().ForgetTaskSafely();
            SyncCollections().ForgetTaskSafely();
            SyncUsersFollowing().ForgetTaskSafely();
            SyncUserCreations().ForgetTaskSafely();

            if (ModioClient.Settings.TryGetPlatformSettings(out MonetizationSettings settings)
                && settings.MonetizationType == ModioMonetizationType.UsdMarketplace
                && ModioServices.TryResolve(out IModioUsdMarketplaceService usdCurrencyProvider))
                await usdCurrencyProvider.UpdateSkuCache();

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
    
            (Error error, List<ModObject> subObjects) = await ModioAPI.CrawlAllPages(filter, ModioAPI.Me.GetUserSubscriptions);

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
            (Error gameDataError, GameData data) = await GameData.GetGameData();

            if (!settings.TryGetPlatformSettings(out MonetizationSettings _)
                || (!gameDataError && data.MonetizationOptions == 0))
            {
                ModioLog.Message?.Log($"No {typeof(MonetizationSettings)} settings found, skipping SyncPurchases");
                return Error.None;
            }

            ModioAPI.Me.GetUserPurchasesFilter filter = ModioAPI.Me.FilterGetUserPurchases().GameId(settings.GameId);

            (Error error, List<ModObject> purchasedObjects) = await ModioAPI.CrawlAllPages(filter, ModioAPI.Me.GetUserPurchases);

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

            (Error error, List<ModCollectionObject> collections) = await ModioAPI.CrawlAllPages(filter, ModioAPI.Me.GetUserFollowedCollections);

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
            (Error gameDataError, GameData data) = await GameData.GetGameData();

            if (!settings.TryGetPlatformSettings(out MonetizationSettings monetizationSettings)
                || (!gameDataError && data.MonetizationOptions == 0))
            {
                ModioLog.Message?.Log($"No {typeof(MonetizationSettings)} settings found, skipping SyncEntitlements");
                return Error.None;
            }

            Error error = Error.None;
            if (monetizationSettings.MonetizationType == ModioMonetizationType.UsdMarketplace)
            {
                ModioLog.Message?.Log($"USD Marketplace monetization does not sync entitlements, caching platform entitlements");
                (error, _)  = await UpdateEntitlementCache();

                if (error && !error.IsSilent) ModioLog.Error?.Log($"Error Updating Entitlements Cache for {UserId}: {error}");
                
                error  = await ModioFiatPrice.FetchSkuCache(ModioAPI.CurrentPortal);
                
                if (error && !error.IsSilent) 
                    ModioLog.Error?.Log($"Error Fetching SKU Cache for {UserId}: {error}");
            }
            
            if (ModioServices.TryResolve(out IModioEntitlementService entitlementPlatform))
                error = await entitlementPlatform.SyncEntitlements();

            if (error && !error.IsSilent) 
                ModioLog.Error?.Log($"Error syncing Entitlements for {UserId}: {error}");

            ModioLog.Verbose?.Log($"Finished Syncing Entitlements {LocalUserId} with result: {Error.None}");

            await SyncWallet();
            return error;
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
            (Error gameDataError, GameData data) = await GameData.GetGameData();

            if (!settings.TryGetPlatformSettings(out MonetizationSettings _)
                || (!gameDataError && data.MonetizationOptions == 0))
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
        public async Task<Error> SyncModRatings()
        {
            ModioLog.Verbose?.Log($"Syncing Ratings {UserId}");

            var settings = ModioServices.Resolve<ModioSettings>();

            var filter = ModioAPI.Me.FilterGetUserRatings();
            filter.GameId(settings.GameId);
            filter.ResourceType(ModioResourceType.Mod);

            (Error error, List<RatingObject> ratedMods) = await ModioAPI.CrawlAllPages(filter, ModioAPI.Me.GetUserRatings);

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error syncing Ratings for {UserId}: {error}");
                return error;
            }
            
            foreach (RatingObject ratingObject in ratedMods)
            {
                _modRatings[ratingObject.ModId] = (ModioRating)ratingObject.Rating;
                
                if (!ModCache.TryGetMod(ratingObject.ModId, out Mod mod))
                    continue;

                mod.SetCurrentUserRating((ModioRating)ratingObject.Rating);
            }
            
            ModioLog.Verbose?.Log($"Finished Syncing Ratings for {LocalUserId} with result: {error}");

            return Error.None;
        }
        
        /// <summary>
        /// Syncs the user ratings with changes made on the WebInterface
        /// </summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        public async Task<Error> SyncCollectionRatings()
        {
            ModioLog.Verbose?.Log($"Syncing Ratings {UserId}");

            var settings = ModioServices.Resolve<ModioSettings>();

            var filter = ModioAPI.Me.FilterGetUserRatings();
            filter.GameId(settings.GameId);
            filter.ResourceType(ModioResourceType.Collection);

            (Error error, List<RatingObject> ratedMods) = await ModioAPI.CrawlAllPages(filter, ModioAPI.Me.GetUserRatings);

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error syncing Ratings for {UserId}: {error}");
                return error;
            }
            
            foreach (RatingObject ratingObject in ratedMods)
            {
                _collectionRatings[ratingObject.ModId] = (ModioRating)ratingObject.Rating;
                
                if (!ModCollectionCache.TryGetCachedStatic(ratingObject.ModId, out ModCollection collection))
                    continue;

                collection.SetCurrentUserRating((ModioRating)ratingObject.Rating);
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

        public async Task<Error> SyncUserCreations()
        {
            var filter = ModioAPI.Me.FilterGetUserMods();
            
            filter.GameId(ModioClient.Settings.GameId);
            //Don't show archived mods; those are only useful on the web interface
            filter.Status(3, Filtering.Not);
            
            (Error error, List<ModObject> modObjects) = await ModioAPI.CrawlAllPages(filter, ModioAPI.Me.GetUserMods);

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error syncing User Creations for {UserId}: {error}");
                return error;
            }
            
            List<Mod> creations = modObjects
                                  .Select(ModCache.GetMod)
                                  .ToList();

            // Get all previously, but not currently, subscribed mods and update their status
            var cachedCreations = new HashSet<Mod>(ModRepository.GetCreatedMods());
            
            // Cull values that don't need to change
            foreach (Mod creation in creations)
            {
                cachedCreations.Remove(creation);
                creation.UpdateLocalCreationStatus(true);
            }

            foreach (Mod mod in cachedCreations) 
                mod.UpdateLocalCreationStatus(false);

            OnUserChanged?.Invoke(this);
            
            ModioLog.Verbose?.Log($"Finished Syncing User Creations for {LocalUserId} with result: {error}");

            return Error.None;
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

        void ClearCachedUserCreations()
        {
            ModCache.ClearStartingWith("me:yes,");
        }

        /// <summary>
        /// Gets all users following the currently authenticated User from the API.
        /// </summary>
        /// <returns>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="IReadOnlyList{UserProfile}"/> results), where:</returns>
        public async Task<(Error error, IReadOnlyList<UserProfile> results)> SyncUsersFollowers()
        {
            ModioAPI.Me.GetUserFollowersFilter filter = ModioAPI.Me.FilterGetUserFollowers();
            
            (Error error, List<UserObject> userObjects) = await ModioAPI.CrawlAllPages(filter, ModioAPI.Me.GetUserFollowers);
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
        public async Task<(Error error, IReadOnlyList<UserProfile> results)> SyncUsersFollowing()
        {
            var followedPreviously = new List<UserProfile>(_followed);
            
            ModioAPI.Me.GetUsersFollowingFilter filter = ModioAPI.Me.FilterGetUsersFollowing();
            
            (Error error, List<UserObject> userObjects) = await ModioAPI.CrawlAllPages(filter, ModioAPI.Me.GetUsersFollowing);
            if (error)
            {
                if (!error.IsSilent) 
                    ModioLog.Error?.Log($"Error getting followed users of user {UserId}: {error}");
                return (error, Array.Empty<UserProfile>());
            }

            List<UserProfile> output = userObjects.Select(UserProfile.Get).ToList();

            // Remove all confirmed still followed, then set remainder as unfollowed
            followedPreviously.RemoveAll(output.Contains);
            foreach (UserProfile user in followedPreviously)
                user.SetFollowed(false);

            // This way we capture changes via web interface
            foreach (UserProfile user in output)
                user.SetFollowed(true);

            _followed = output;

            return (Error.None, output);
        }

        public async Task<Error> FollowUser(UserProfile user)
        {
            var request = new FollowUserRequest(user.UserId);
            (Error error, Response204? _) = await ModioAPI.Followers.FollowUser(UserId, request);

            if (error && error.Code != ErrorCode.USER_TARGET_ALREADY_FOLLOWED)
                return error;

            if (!_followed.Contains(user))
                _followed.Add(user);
            
            user.SetFollowed(true);

            return error;
        }

        public async Task<Error> UnfollowUser(UserProfile user)
        {
            (Error error, Response204? _) = await ModioAPI.Followers.UnfollowUser(UserId, user.UserId);
            
            if (error && error.Code != ErrorCode.ALREADY_UNSUBSCRIBED)
                return error;

            _followed.Remove(user);
            user.SetFollowed(false);

            return error;
        }

        public bool TryGetRating(ModioId id, ModioResourceType resourceType, out ModioRating rating)
            => resourceType switch
            {
                ModioResourceType.Mod => _modRatings.TryGetValue(id, out rating),
                ModioResourceType.Collection => _collectionRatings.TryGetValue(id, out rating),
                _ => throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null),
            };

        /// <summary>Removes the <see cref="User"/> and associated authentication and caches from this device.</summary>
        [ModioDebugMenu(ShowInBrowserMenu = false, ShowInSettingsMenu = true)]
        public static void DeleteUserData()
        {
            var dataStorage = ModioServices.Resolve<IModioDataStorage>();

            dataStorage.DeleteUserData(User.Current.LocalUserId).ForgetTaskSafely();
            
            LogOut().ForgetTaskSafely();
        }

        /// <summary>Logs out the current <see cref="User"/> without deleting any associated data stored on this device.</summary>
        public static async Task LogOut()
        {
            ModioLog.Verbose?.Log($"Logging out {Current?.LocalUserId}");
            Current?.ModRepository.Dispose();

            ResetModsStatus();
            
            if (Current?._writeTcs is not null)
                await Current._writeTcs.Task;
            
            Current = new User();
            OnUserChanged?.Invoke(Current);
        }

        static void ResetModsStatus()
        {
            if (Current?.ModRepository == null)
                return;

            foreach (Mod mod in Current.ModRepository.GetSubscribed())
                mod.UpdateLocalSubscriptionStatus(false);

            foreach (Mod mod in Current.ModRepository.GetPurchased())
                mod.UpdateLocalPurchaseStatus(false);

            foreach (Mod mod in Current.ModRepository.GetDisabled())
                mod.UpdateLocalEnabledStatus(true);
        }

        /// <summary>
        /// Sets the current user's auth token to an invalid value
        /// Used for testing error handling
        /// </summary>
        [ModioDebugMenu(ShowInBrowserMenu = true, ShowInSettingsMenu = false)]
        public static void InvalidateAuthToken()
        {
            Current._authentication.OAuthToken = "INVALID_TOKEN"; // Invalid token
            Current.SaveUserData().ForgetTaskSafely();
        }

        internal async Task<Error> SaveUserData()
        {
            if (!ModioClient.IsInitialized && !ModioClient.IsCurrentlyInitializing)
                return new Error(ErrorCode.NOT_INITIALIZED);
            
            if (_writeTcs is not null)
            {
                _needsSavingToDisk = true;
                return await _writeTcs.Task;
            }
            
            _needsSavingToDisk = false;
            _writeTcs = new TaskCompletionSource<Error>();
            Error error;

            do
            {
                _needsSavingToDisk = false;
                error = await ModioClient.DataStorage.WriteUserData(GetWritable());
            }
            while (_needsSavingToDisk && !error && (ModioClient.IsInitialized || ModioClient.IsCurrentlyInitializing));

            _writeTcs.SetResult(error);
            _writeTcs = null;

            return error;
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
                UserPortal = (int)AuthenticatedPortal,
            };

        /// <summary>
        /// Cached entitlements the user has
        /// </summary>
        /// <returns></returns>
        List<CachedEntitlement> GetCachedEntitlements() => _cachedEntitlements;

        /// <summary>
        /// Update the local cached entitlements from the API
        /// </summary>
        /// <returns></returns>
        async Task<(Error, List<CachedEntitlement>)> UpdateEntitlementCache()
        {
            if (!ModioServices.TryResolve(out IModioUsdMarketplaceService service))
            {
                ModioLog.Error?.Log("Unable to resolve IModioUsdMarketplaceService to update entitlement cache");
                return (new Error(ErrorCode.UNKNOWN), null);
            }
            
            (Error error, List<BrowseEntitlementObject> entitlements) = await service.GetAllUserEntitlements();
            
            if (error)
                return (error, null);

            _cachedEntitlements = new List<CachedEntitlement>();

            foreach (BrowseEntitlementObject t in entitlements)
                _cachedEntitlements.Add(new CachedEntitlement(t));
            
            return (Error.None, _cachedEntitlements);
        }

        

        /// <summary>
        /// Removes the local cached entitlement with the given skuId
        /// </summary>
        /// <param name="skuId"></param>
        void ConsumeCachedEntitlement(string skuId)
        {
            CachedEntitlement entitlement = _cachedEntitlements.First(
                entitlement => string.Equals(entitlement.SkuId, skuId, StringComparison.OrdinalIgnoreCase)
            );

            _cachedEntitlements.Remove(entitlement);
        }

        internal async Task<(Error error, PayObject? payObject)> PurchaseModWithUsdMarketplace(
            Mod mod,
            bool subscribeOnPurchase
        ) {
            var service = ModioServices.Resolve<IModioUsdMarketplaceService>();
            
            if (service == null)
                return (new Error(ErrorCode.UNKNOWN), null);

            Error error = Error.None;
            
            //Check cached entitlements
            //if none, get entitlements from modio
            List<CachedEntitlement> entitlements = GetCachedEntitlements();
            
            if (entitlements == null || entitlements.Count == 0)
                (error, entitlements) = await UpdateEntitlementCache();
            
            if (error)
                return (error,null);

            if(entitlements == null || entitlements.Count == 0)
            {
                error = await service.OpenPurchaseFlow(mod.PortalSku);

                if (error)
                    return (error,null);

                (error, _) = await UpdateEntitlementCache();

                entitlements = GetCachedEntitlements();
                
                if (error)
                    return ( error,null);

                if (entitlements == null || entitlements.Count == 0)
                {
                    error = Error.Unknown;
                    ModioLog.Error?.Log($"Unknown error receiving entitlements: Received no entitlements after purchasing SKU {mod.PortalSku}. Please try again later.");
                    return (error, null);
                }
            }
            
            string idempotent = $"{mod.Id}";

            PayObject? payObject;
            (error, payObject) = await service.TryPurchase(mod, subscribeOnPurchase, idempotent);

            if (error)
                return (error, payObject);

            ConsumeCachedEntitlement(mod.PortalSku.Sku);
            return (Error.None, payObject);
        }

        internal async Task<(Error error, PayObject? payObject)> PurchaseModWithVirtualCurrency(Mod mod, bool subscribeOnPurchase)
        {
            var idempotent = $"{mod.Id}";

            (Error error, PayObject? payObject) = await ModioAPI.Monetization.Purchase(
                mod.Id,
                new PayRequest(
                    mod.Price,
                    subscribeOnPurchase,
                    idempotent,
                    0));
            
            if (error)
                return (error, null);

            ApplyWalletFromPurchase(payObject.Value);

            return (Error.None, payObject);
        }
    }

    public struct CachedEntitlement
    {
        public readonly long EntitlementType;
        public readonly string SkuId;
        
        public CachedEntitlement(BrowseEntitlementObject sourceEntitlement)
        {
            EntitlementType = sourceEntitlement.EntitlementType;
            SkuId = sourceEntitlement.SkuId;
        }
    }

    [Serializable]
    public class UserSaveObject
    {
        public string LocalUserId;
        public string Username;
        public long UserId;
        public string AuthToken;
        public List<long> SubscribedMods;
        public List<long> DisabledMods;
        public List<long> PurchasedMods;
        public List<long> FollowedCollections;
        public int UserPortal;
    }
}
