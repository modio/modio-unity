using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Authentication;
using Modio.Caching;
using Modio.Errors;
using Modio.Extensions;
using Modio.Images;
using Modio.Mods.Builder;
using Modio.Monetization;
using Modio.Reports;
using Modio.Users;
using Plugins.Modio.Modio.Ratings;

namespace Modio.Mods
{
    /// <summary>A mod.io mod. This is a mutable class that will be maintained and updated as more data arrives. See
    /// <see cref="OnModUpdated"/> &amp; <see cref="AddChangeListener"/> for listening to these changes and updating
    /// UI elements accordingly.</summary>
    public class Mod : IModioInfo
    {
        /// <summary>Posts an event whenever this mod has been updated.</summary>
        public event Action OnModUpdated;

        /// <summary>Adds an event handler to listen for whenever the <see cref="ModChangeType"/> of a mod
        /// is changed.</summary>
        /// <remarks><see cref="ModChangeType"/> is a bit flag, multiple changes can be listened for with one
        /// handler.</remarks>
        public static void AddChangeListener(ModChangeType subscribedChange, Action<Mod, ModChangeType> listener)
        {
            if (ChangeSubscribers.TryGetValue(subscribedChange, out Action<Mod, ModChangeType> existing)) 
                listener = existing + listener;

            ChangeSubscribers[subscribedChange] = listener;
        }
        
        public static void RemoveChangeListener(ModChangeType subscribedChange, Action<Mod, ModChangeType> listener)
        {
            if (ChangeSubscribers.TryGetValue(subscribedChange, out Action<Mod, ModChangeType> _)) 
                ChangeSubscribers[subscribedChange] -= listener;
        }
        
        static readonly Dictionary<ModChangeType, Action<Mod, ModChangeType>> ChangeSubscribers =
            new Dictionary<ModChangeType, Action<Mod, ModChangeType>>();

        /// <summary>Constructs a <see cref="ModBuilder"/> to use to create a new mod.</summary>
        /// <remarks>Requires a <see cref="ChangeFlags.Name"/>, a <see cref="ChangeFlags.Summary"/> and
        /// a <see cref="ChangeFlags.Logo"/> to publish a new mod.</remarks>
        public static ModBuilder Create() => new ModBuilder();
        
        /// <summary>Constructs a <see cref="ModBuilder"/> to use to edit this mod.</summary>
        public ModBuilder Edit() => new ModBuilder(this);
        
        /*  Mod Properties  */
        public ModioId Id { get; }
        public string Name { get; private set; }
        public string Summary => _summaryDecoded ??= WebUtility.HtmlDecode(_summaryHtmlEncoded);
        public string Description { get; private set; }
        public DateTime DateLive { get; private set; }
        public DateTime DateUpdated { get; private set; }
        public ModTag[] Tags { get; private set; }
        public ModSku PortalSku { get; private set; }
        public string MetadataBlob { get; private set; }
        public Dictionary<string,string> MetadataKvps { get; private set; }
        public ModCommunityOptions CommunityOptions { get; private set; }
        public ModMaturityOptions MaturityOptions { get; private set; }
        public Modfile File { get; private set; }
        public ModPlatform[] SupportedPlatforms { get; private set; }
        public ModStats Stats { get; private set; }
        public long Price { get; private set; }
        public bool IsMonetized { get; private set; }

        public ModioImageSource<LogoResolution> Logo { get; private set; }
        public ModioImageSource<GalleryResolution>[] Gallery { get; private set; }
        public UserProfile Creator { get; private set; }
        public ModDependencies Dependencies { get; private set; }
        public ModioRating CurrentUserRating { get; private set; }

        public bool IsSubscribed { get; private set; }
        public bool IsPurchased { get; private set; }
        public bool IsEnabled { get; internal set; } = true;
        /// <summary>
        /// Is the mod a user creation for the currently authenticated user
        /// </summary>
        public bool IsCreated { get; private set; }

        /*  Mod Properties End  */

        string _summaryDecoded;
        string _summaryHtmlEncoded;

        internal ModObject LastModObject { get; private set; }

        public string FiatPrice { get; internal set; }

        public enum LogoResolution
        {
            X320_Y180,
            X640_Y360,
            X1280_Y720,
            Original,
        }

        public enum GalleryResolution
        {
            X320_Y180,
            X1280_Y720,
            Original,
        }

        internal Mod(ModioId id) => Id = id;

        internal Mod(ModObject modObject)
        {
            Id = modObject.Id;
            ApplyDetailsFromModObject(modObject);
        }

        public static Mod Get(long id) => ModCache.GetMod(new ModioId(id));

        internal Mod ApplyDetailsFromModObject(ModObject modObject)
        {
            

            Name = WebUtility.HtmlDecode(modObject.Name);
            _summaryHtmlEncoded = modObject.Summary;
            _summaryDecoded = null;
            Description = modObject.DescriptionPlaintext;

            Creator = UserProfile.Get(modObject.SubmittedBy);

            DateLive = modObject.DateLive.GetLocalDateTime();
            DateUpdated = modObject.DateUpdated.GetLocalDateTime();

            Tags = modObject.Tags?.Select(ModTag.Get).ToArray() ?? Array.Empty<ModTag>();
            
            MetadataBlob = modObject.MetadataBlob;

            MetadataKvps ??= new Dictionary<string, string>();
            MetadataKvps.Clear();
            // Note that the server can store duplicate keys; we're only tracking the most recent one here
            foreach (MetadataKvpObject metadataKvpObject in modObject.MetadataKvp)
                MetadataKvps[metadataKvpObject.Metakey] = metadataKvpObject.Metavalue;

            CommunityOptions = (ModCommunityOptions)modObject.CommunityOptions;
            MaturityOptions = (ModMaturityOptions)modObject.MaturityOption;

            if (File == null) 
                File = new Modfile(modObject.Modfile);
            else if (modObject.Modfile.Id != 0) // We get a blank Modfile from some operations like Subscribe
                File.ApplyDetailsFromModfileObject(modObject.Modfile);
            SupportedPlatforms = modObject.Platforms.Select(platformObject => new ModPlatform(platformObject, Id)).ToArray();

            if (User.Current.TryGetRating(Id, ModioResourceType.Mod, out ModioRating rating))
                CurrentUserRating = rating;

            Stats = new ModStats(modObject.Stats, CurrentUserRating);
            IsMonetized = ((int)modObject.MonetizationOptions & (int)ModMonetizationOption.Enabled) != 0
                          && ((int)modObject.MonetizationOptions & (int)ModMonetizationOption.Live) != 0;
            Price = modObject.Price;
            
            // If the price is 0, the mod cannot be monetized
            if(Price == 0) 
                IsMonetized = false;
            
            if (Logo == null || string.IsNullOrEmpty(Logo.FileName)) 
                Logo = new ModioImageSource<LogoResolution>(
                    modObject.Logo.Filename,
                    modObject.Logo.Thumb320X180,
                    modObject.Logo.Thumb640X360,
                    modObject.Logo.Thumb1280X720,
                    modObject.Logo.Original
                );

            if (modObject.Skus != null && modObject.Skus.Length != 0 && IsMonetized)
                GetFiatPrice(modObject);
            
            Gallery = modObject.Media.Images.Select(
                                   imageObject => new ModioImageSource<GalleryResolution>(
                                       imageObject.Filename,
                                       imageObject.Thumb320X180,
                                       imageObject.Thumb1280X720,
                                       imageObject.Original
                                   )
                               )
                               .ToArray();

            if(Dependencies == null || !Dependencies.HasDependencies)
                Dependencies = new ModDependencies(this, modObject.Dependencies);

            LastModObject = modObject;
            
            InvokeModUpdated(ModChangeType.Everything);
            return this;
        }

        void GetFiatPrice(ModObject modObject)
        {

            ModSkuObject skuObject =
                modObject.Skus.FirstOrDefault(
                    sku => string.Equals(
                        sku.Portal,
                        ModioAPI.CurrentPortal.GetHeader(),
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            // If Id is 0, no matching SKU was found for this portal
            PortalSku = skuObject.Id == 0 ? null : new ModSku(skuObject);
            
            ModioFiatPrice.TryGetLocalPrice(this);
        }

#region Subscriptions

        public Task<Error> Subscribe(bool includeDependencies = true) => SetSubscribed(true, includeDependencies);

        public Task<Error> Unsubscribe() => SetSubscribed(false);

        async Task<Error> SetSubscribed(bool subscribed, bool includeDependencies = true)
        {
            if (!User.Current.IsAuthenticated)
                return new Error(ErrorCode.USER_NOT_AUTHENTICATED);

            if (IsSubscribed == subscribed)
                return Error.None;

            //Set pending subscribe
            UpdateLocalSubscriptionStatus(subscribed);

            Error error;

            if (subscribed)
            {

                ModObject? modObject;

                (error, modObject) = await ModioAPI.Subscribe.SubscribeToMod(
                    Id,
                    new AddModSubscriptionRequest(includeDependencies)
                );

                if (modObject.HasValue) ApplyDetailsFromModObject(modObject.Value);
            }
            else
                (error, _) = await ModioAPI.Subscribe.UnsubscribeFromMod(Id);

            

            //error handling for subscribing; set back to previous state
            if (subscribed && error)
            {
                UpdateLocalSubscriptionStatus(false);
                return error;
            }

            //check space available
            if (subscribed)
            {
                bool spaceAvailable = await ModInstallationManagement.IsThereAvailableSpaceFor(this);

                if (!spaceAvailable)
                {
                    error = new FilesystemError(FilesystemErrorCode.INSUFFICIENT_SPACE);

                    File.FileStateErrorCause = error;
                    File.State = ModFileState.FileOperationFailed;

                    InvokeModUpdated(ModChangeType.FileState);

                    return error;
                }
            }
            
            
            // Note that we allow unsubscribe to continue during a disconnection, so the user can uninstall mods while offline
            if (!subscribed && error && error.Code != ErrorCode.CANNOT_OPEN_CONNECTION)
            {
                UpdateLocalSubscriptionStatus(true);
                return error;
            }

            if (subscribed && includeDependencies)
            {
                (Error dependencyError, IEnumerable<Mod> dependencies) = await Dependencies.GetAllDependencies();

                if (dependencies != null && !dependencyError)
                    foreach (Mod dependency in dependencies)
                    {
                        dependency.UpdateLocalSubscriptionStatus(true);
                    }
            }
            

            InvokeModUpdated(ModChangeType.IsSubscribed);
            
            ModInstallationManagement.WakeUp();

            return error;
        }

        public void SetIsEnabled(bool isEnabled) => UpdateLocalEnabledStatus(isEnabled);

        internal void UpdateLocalSubscriptionStatus(bool isSubscribed)
        {
            if(IsSubscribed == isSubscribed) return;
            IsSubscribed = isSubscribed;
            InvokeModUpdated(ModChangeType.IsSubscribed);
        }

        internal void UpdateLocalEnabledStatus(bool isEnabled)
        {
            IsEnabled = isEnabled;
            InvokeModUpdated(ModChangeType.IsEnabled);
        }

        internal void UpdateLocalPurchaseStatus(bool isPurchased)
        {
            IsPurchased = isPurchased;
            InvokeModUpdated(ModChangeType.IsPurchased);  
        }

        internal void UpdateLocalCreationStatus(bool isCreated)
        {
            IsCreated = isCreated;
            InvokeModUpdated(ModChangeType.IsUserCreation);
        }
#endregion

#region GetMods/Mod

        /// <summary>Gets all mods that qualify the provided <see cref="ModSearchFilter"/> parameters.</summary>
        /// <remarks>This will cache searches and results. If a search exists in the cache, this method will
        /// return those results.</remarks>
        public static Task<(Error error, ModioPage<Mod> page)> GetMods(ModSearchFilter filter)
            => GetMods(filter.GetModsFilter());
        
        /// <summary>Gets all mods that qualify the provided <see cref="ModioAPI.Mods.GetModsFilter"/> parameters.</summary>
        /// <remarks>This will cache searches and results. If a search exists in the cache, this method will
        /// return those results.<br/>
        /// The <see cref="ModioAPI.Mods.GetModsFilter"/> is more advanced than the <see cref="ModSearchFilter"/> and
        /// will allow more granularity.</remarks>
        public static async Task<(Error error, ModioPage<Mod> page)> GetMods(ModioAPI.Mods.GetModsFilter filter)
        {
            string searchCacheKey = ModCache.ConstructFilterKey(filter);

            if (ModCache.GetCachedModSearch(filter, searchCacheKey, out Mod[] cachedMods, out long resultTotal))
            {
                var pagination = new ModioPage<Mod>(
                    cachedMods,
                    filter.PageSize,
                    filter.PageIndex,
                    resultTotal
                );

                return (Error.None, pagination);
            }

            (Error error, Pagination<ModObject[]>? modObjects) = await ModioAPI.Mods.GetMods(filter);

            if (error) return (error, null);

            ModioPage<Mod> page = ConvertPaginationToModPage(modObjects.Value, searchCacheKey);

            return (Error.None, page);
        }

        internal static ModioPage<Mod> ConvertPaginationToModPage(Pagination<ModObject[]> modObjects, string searchCacheKey)
        {
            int resultCount = modObjects.Data.Length;
            Mod[] mods = resultCount == 0 ? Array.Empty<Mod>() : new Mod[resultCount];

            for (var i = 0; i < mods.Length; i++) mods[i] = ModCache.GetMod(modObjects.Data[i]);

            long pageSize = modObjects.ResultLimit;
            long pageIndex = modObjects.ResultOffset / pageSize;

            if(!string.IsNullOrEmpty(searchCacheKey))
                ModCache.CacheModSearch(searchCacheKey, mods, pageIndex, modObjects.ResultTotal);

            var page = new ModioPage<Mod>(
                mods,
                (int)pageSize,
                pageIndex,
                modObjects.ResultTotal
            );
            return page;
        }

        /// <summary>Gets this mod's details from the mod.io server and applies those details to this mod.</summary>
        /// <returns>An <see cref="Error"/> with an Error Code if there was one.</returns>
        public async Task<(Error error, Mod result)> GetModDetailsFromServer()
        {
            (Error error, ModObject? modObject) = await ModioAPI.Mods.GetMod(Id);

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error getting Mod {Id}: {error.GetMessage()}");
                return (error, this);
            }

            ApplyDetailsFromModObject(modObject.Value);

            return (error, this);
        }

        /// <summary>Gets a Mod from the mod.io server. Will check the cache if a concrete object has already been
        /// cached. If one has been cached, it will apply the details to the found concrete mod.</summary>
        public static async Task<(Error error, Mod result)> GetMod(ModioId modId)
        {
            if (!modId.IsValid()) return (new Error(ErrorCode.BAD_PARAMETER), null);

            if (ModCache.TryGetMod(modId, out Mod output) && !string.IsNullOrEmpty(output.LastModObject.Name)) return (Error.None, output);

            (Error error, ModObject? modObject) = await ModioAPI.Mods.GetMod(modId);

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error getting Mod {modId}: {error.GetMessage()}");
                return (error, null);
            }

            output = ModCache.GetMod(modObject.Value);

            return (error, output);
        }

        /// <summary>Gets all mods from a collection of IDs. Will intelligently check the cache for any present mods
        /// and only get the mods that're missing from the cache.</summary>
        /// <param name="neededModIds">A collection of <see cref="ModioId"/>s or longs to get.</param>
        /// <remarks>This can be considered a method to guarantee that the mods are in the cache by completion
        /// of this task. Use this whenever you don't know if you'll have the mod data ready but need to be certain
        /// it's available.</remarks>
        /// <returns>A collection of <see cref="Mod"/>s.</returns>
        public static async Task<(Error error, ICollection<Mod>)> GetMods(ICollection<long> neededModIds)
        {
            if (neededModIds.Count <= 0) 
                return (Error.None, Array.Empty<Mod>());
            
            var output = new List<Mod>(neededModIds.Count);
            var idsToFetch = new List<long>();

            foreach (long id in neededModIds)
                if (ModCache.TryGetMod(id, out Mod mod) && mod.File != null)
                    output.Add(mod);
                else
                    idsToFetch.Add(id);
            
            if(idsToFetch.Count == 0)
                return (Error.None, output);
            
            ModioAPI.Mods.GetModsFilter filter = ModioAPI.Mods.FilterGetMods();
            filter.Id(idsToFetch, Filtering.In);
            filter.RevenueType((long)RevenueType.FreeAndPaid);

            while (true)
            {
                (Error error, ModioPage<Mod> modPage) = await Mod.GetMods(filter);

                if (error)
                {
                    if (!error.IsSilent) 
                        ModioLog.Error?.Log($"Error getting Mods to populate cache from Index: {error.GetMessage()}");
                    return (error, Array.Empty<Mod>());
                }

                output.AddRange(modPage.Data);
                
                if (modPage.HasMoreResults())
                    filter.PageIndex += 1;
                else
                    break;
            }

            return (Error.None, output);
        }

#endregion

        /// <summary>Rate this mod as either a single positive, negative or no rating.</summary>
        public async Task<Error> RateMod(ModioRating rating)
        {
            var previousRating = CurrentUserRating;
            
            UpdateStatsWithUserRating(rating);
            InvokeModUpdated(ModChangeType.Rating);

            var body = new AddRatingRequest((long)rating);

            (Error error, _) = await ModioAPI.Ratings.AddModRating(Id, body);

            if (error)
            {
                if (error.Code is ErrorCode.USER_EXISTING_MOD_RATING or ErrorCode.USER_NO_MOD_RATING)
                {
                    ModioLog.Warning?.Log($"Mod {this}: couldn't rate {rating} due to {error}");
                    return Error.None;
                }

                if (!error.IsSilent) ModioLog.Warning?.Log($"Error rating mod {Id}: {error.GetMessage()}");
                UpdateStatsWithUserRating(previousRating);
                InvokeModUpdated(ModChangeType.Rating);
            }

            return error;
            
            void UpdateStatsWithUserRating(ModioRating userRating)
            {
                Stats.UpdateEstimateFromLocalRatingChange(userRating);
                CurrentUserRating = userRating;
                InvokeModUpdated(ModChangeType.Rating);
            }
        }

        internal void SetCurrentUserRating(ModioRating rating)
        {
            CurrentUserRating = rating;
            Stats?.UpdatePreviousRating(rating);
            InvokeModUpdated(ModChangeType.Rating);
        }

        /// <summary>Reports this mod to mod.io. <see cref="ReportType"/> &amp; <see cref="ModNotWorkingReason"/> for
        /// report reasons.</summary>
        public async Task<Error> Report(
            ReportType reportType,
            ModNotWorkingReason reportReason,
            string contact,
            string summary
        ) {
            if (User.Current == null || !User.Current.IsAuthenticated) return (Error)ErrorCode.USER_NOT_AUTHENTICATED;

            var request = new AddReportRequest(
                ReportResourceTypes.MODS,
                Id,
                (long)reportType,
                (long)reportReason,
                null,
                User.Current.Profile.Username,
                contact,
                summary
            );

            var (error, response) = await ModioAPI.Reports.SubmitReport(request);
            return (Error)error;
        }
        
        internal void InvokeModUpdated(ModChangeType changeFlags)
        {
            foreach ((ModChangeType changeType, Action<Mod, ModChangeType> listeners) in ChangeSubscribers)
                if ((changeType & changeFlags) != 0) 
                    listeners?.Invoke(this, changeFlags);
            
            OnModUpdated?.Invoke();
        }

        internal void UpdateModfile(Modfile modfile)
        {
            File = modfile;
            InvokeModUpdated(ModChangeType.Modfile);
        }

        public async Task<Error> Purchase(bool subscribeOnPurchase)
        {
            var filter = new ModioAPI.Me.GetUserPurchasesFilter(0, 1);
            filter.Id(Id);
            filter.GameId(ModioClient.Settings.GameId);
            (Error error, Pagination<ModObject[]>? pages) = await ModioAPI.Me.GetUserPurchases(filter);
            
            if (!error && pages.Value.Data.Length > 0)
            {
                ModioLog.Message?.Log("Mod already purchased according to user purchases endpoint, applying purchase locally.");
                ModObject modObject = pages.Value.Data[0];
                ApplyDetailsFromModObject(modObject);
                UpdateLocalPurchaseStatus(true);
                if (subscribeOnPurchase) UpdateLocalSubscriptionStatus(true);
                return Error.None;
            }

            if(!ModioServices.Resolve<ModioSettings>().TryGetPlatformSettings(out MonetizationSettings settings))
                return new Error(ErrorCode.GAME_MONETIZATION_NOT_ENABLED);

            PayObject? payObject;
            ( error, payObject) = settings.MonetizationType switch
            {
                ModioMonetizationType.VirtualCurrency => await PurchaseWithVirtualCurrency(subscribeOnPurchase),
                ModioMonetizationType.UsdMarketplace  => await PurchaseWithUsdMarketplace(subscribeOnPurchase),
                _                                     => (new Error(ErrorCode.GAME_MONETIZATION_NOT_CONFIGURED), null),
            };
            
            if(error)
                return error;
            
            ApplyPurchase(subscribeOnPurchase, payObject.Value);
            return Error.None;
        }

        async Task<(Error error, PayObject? payObject)> PurchaseWithUsdMarketplace(bool subscribeOnPurchase)
        {
            if (PortalSku == null)
                return (new Error(ErrorCode.MONETIZATION_ENTITLEMENT_MAPPING_NOT_FOUND), null);
            
            (Error error, PayObject? payObject) = await User.Current.PurchaseModWithUsdMarketplace(this, subscribeOnPurchase);

            if (!error)
                return (Error.None, payObject);

            if (!error.IsSilent)
                ModioLog.Error?.Log($"Error purchasing mod {Id}: {error}");
            return (error, null);
        }

        async Task<(Error error, PayObject? payObject)> PurchaseWithVirtualCurrency(bool subscribeOnPurchase)
        {
            (Error error, PayObject? payObject) = await User.Current.PurchaseModWithVirtualCurrency(this, subscribeOnPurchase);

            if (!error)
                return (Error.None, payObject);

            if (!error.IsSilent)
                ModioLog.Error?.Log($"Error purchasing mod {Id}: {error}");
            return (error, null);
            
        }

        void ApplyPurchase(bool subscribe, PayObject payObject)
        {
            IsPurchased = true;
            var modChangeType = ModChangeType.IsPurchased;

            if (subscribe)
            {
                IsSubscribed = true;
                modChangeType |= ModChangeType.IsSubscribed;
            }
            
            ModObject? modObject = payObject.Mod;
            ApplyDetailsFromModObject(modObject.Value);
            InvokeModUpdated(modChangeType);
            ModInstallationManagement.WakeUp();
        }

        /// <summary>Will enqueue this mod for uninstallation with <see cref="ModInstallationManagement"/>.</summary>
        /// <remarks>If the mod is subscribed to, this operation will cancel and output a warning.</remarks>
        public void UninstallOtherUserMod()
        {
            if (IsSubscribed)
            {
                ModioLog.Warning?.Log($"Attempting to uninstall mod {Id}, but its subscribed. Cancelling.");
                return;
            }
            
            ModInstallationManagement.MarkModForUninstallation(this);
        }

        public override string ToString() => $"Mod({Id}:{Name})";
    }
}
