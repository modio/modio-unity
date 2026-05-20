using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Modio.API;
using Modio.Collections;
using Modio.Errors;
using Modio.Extensions;
using Modio.Mods;
using Modio.Monetization;
using Modio.Unity.UI.Components;
using Modio.Unity.UI.Components.SearchProperties;
using Modio.Users;
using UnityEngine;
using UnityEngine.Events;
using SearchFilter = Modio.Mods.ModSearchFilter;
using UserProfile = Modio.Users.UserProfile;

namespace Modio.Unity.UI.Search
{
    /// <summary>
    /// This manages any searches run by the plugin.
    /// Children of this script can use a <see cref="ModioUISearchProperties"/> to respond to the search updating
    /// Use <see cref="ModioUISearchSettings"/> to specify what you are searching for
    ///
    /// A modioUiSearch is used for the main search by the plugin (the browse screen) as well as a separate one per carousel
    /// It's also used for showing a mods dependencies and a collection's contents
    ///
    /// To get a basic search going;
    ///   - Assign a prefab with a <see cref="ModioUISearchSettings"/> to _searchOnStart
    ///   - Have a child gameObject in the scene with a <see cref="ModioUISearchProperties"/> component
    ///   - Add a <see cref="SearchPropertyDisplayResults"/> to the UISearchProperties
    ///   - Point that DisplayResults at a <see cref="ModioUIModGroup"/>
    /// </summary>
    /// <remarks>In the future, this is planned to be split into ModioUISearch and ModioSearch, which will live in the core of the mod.io C# plugin</remarks>
    public class ModioUISearch : MonoBehaviour, IModioUIPropertiesOwner
    {
        [SerializeField] bool _isDefault = true;

        [Header("Optional Overrides")]
        [SerializeField]
        ModioUISearchSettings _searchOnStart;
        [SerializeField] ModioUISearchSettings _searchForUser;
        [SerializeField] ModioUISearchSettings _searchForTag;
        [SerializeField] int _defaultPageSize = 24;
        [SerializeField, Tooltip("Allow search to run before we have an authenticated user")]
        bool _allowSearchWithoutUser;

        SpecialSearchType _searchPreset;

        public static ModioUISearch Default { get; private set; }

        public SearchFilter LastSearchFilter { get; private set; } = new SearchFilter();
        public SpecialSearchType LastSearchPreset => _searchPreset;

        public bool IsSearching { get; private set; }
        public bool IsAdditiveSearch { get; private set; }
        public IReadOnlyList<Mod> LastSearchResultMods { get; private set; } = new Collection<Mod>();
        public IReadOnlyList<ModCollection> LastSearchResultModCollections { get; private set; } = new Collection<ModCollection>();
        public int LastSearchResultTotalCount { get; private set; }
        int ResultsOnCurrentPageCount => Math.Max(LastSearchResultMods.Count, LastSearchResultModCollections.Count);
        public int LastSearchResultPageCount => Mathf.CeilToInt(
            LastSearchResultTotalCount / (float)Mathf.Max(LastSearchFilter.PageSize, ResultsOnCurrentPageCount)
        );
        public bool CanGetMoreResults =>
            LastSearchResultMods != null && LastSearchResultTotalCount > ResultsOnCurrentPageCount;
        public Error LastSearchError { get; private set; } = Error.None;
        public int LastSearchSelectionIndex { get; private set; }
        public ModioUISearchSettings LastSearchSettingsFrom { get; private set; }

        public bool SortByOverriden { get; private set; }

        public int DefaultPageSize => _defaultPageSize;

        public UnityEvent OnSearchUpdatedUnityEvent;
        (SearchFilter searchFilter, SpecialSearchType specialSearchType, object shareFiltersWith) _resetToSearch;
        (SearchFilter searchFilter, SpecialSearchType specialSearchType) _baseForCustomSearch;
        int _lastPageIndex;
        int _asyncSearchIndex;
        object _shareFiltersWith;
        
        //for quickly grabbing the next page, without recalculating or applying modifications from unsubscribe/uninstall
        List<Mod> _lastLocalQueryInFull;

        public event Action AppliedSearchPreset;

        void Awake()
        {
            if (_isDefault || Default == null) Default = this;
        }

        void OnDestroy()
        {
            ModioClient.OnInitialized -= PluginReady;

            //Ensure we clear the instance, so GC can clean up this object
            if (Default == this) Default = null;
            
            User.OnUserChanged -= PluginReady;
        }

        void Start()
        {
            ModioClient.OnInitialized += PluginReady;

        }

        void PluginReady(User _) => PluginReady();
        void PluginReady()
        {
            User.OnUserChanged -= PluginReady;
            if (!_allowSearchWithoutUser && (User.Current == null || !User.Current.IsInitialized))
            {
                LastSearchResultMods = new Collection<Mod>();
                User.OnUserChanged += PluginReady;
                return;
            }

            if (_resetToSearch.searchFilter != null)
                ClearSearch();
            else if (_searchOnStart != null)
                _searchOnStart.Search(this);
            else
            {
                LastSearchResultMods = new Collection<Mod>();
                OnSearchUpdatedUnityEvent.Invoke();
            }
        }

        public void AddUpdatePropertiesListener(UnityAction listener)
        {
            OnSearchUpdatedUnityEvent.AddListener(listener);
        }

        public void RemoveUpdatePropertiesListener(UnityAction listener)
        {
            OnSearchUpdatedUnityEvent.RemoveListener(listener);
        }

        public void ApplySortBy(SortModsBy sortModsBy, bool ascending)
        {
            SortByOverriden = true;
            LastSearchFilter.SortBy = sortModsBy;
            LastSearchFilter.IsSortAscending = ascending;

            LastSearchFilter.PageIndex = 0;
            SetSearch(LastSearchFilter).ForgetTaskSafely();
        }

        public void ApplySearchPhrase(string query)
        {
            SearchFilter searchFilter = LastSearchFilter;

            if (_baseForCustomSearch.searchFilter != null && _baseForCustomSearch.searchFilter != searchFilter)
            {
                searchFilter = _baseForCustomSearch.searchFilter;
                _searchPreset = _baseForCustomSearch.specialSearchType;
            }

            var filterType = Filtering.Like;
            searchFilter.ClearSearchPhrases(filterType);

            if (!string.IsNullOrEmpty(query))
            {
                searchFilter.AddSearchPhrase(query, filterType);
            }
            if (_searchPreset == SpecialSearchType.SubSearchesOnly) _searchPreset = SpecialSearchType.Nothing;

            searchFilter.PageIndex = 0;
            SetSearch(searchFilter).ForgetTaskSafely();
        }

        public void ApplyTagsToSearch(IEnumerable<ModTag> tags)
        {
            ModTag[] hiddenTags = LastSearchFilter.GetTags().Where(t=>!t.IsVisible).Distinct().ToArray();

            LastSearchFilter.ClearTags();
            LastSearchFilter.AddCollectionCategory(null);

            var nonCategoryTags = new List<ModTag>(hiddenTags);
            
            foreach (ModTag modTag in tags)
            {
                if(nonCategoryTags.Contains(modTag)) continue;
                
                if(modTag.TagType is ResourceTagType.CollectionCategory)
                    LastSearchFilter.AddCollectionCategory(modTag.ApiName);
                else
                    nonCategoryTags.Add(modTag);
            }

            LastSearchFilter.AddTags(nonCategoryTags);

            //If we were doing a tag based search, and we just removed all visible tags, clear search instead
            if (_searchPreset == SpecialSearchType.SearchForTag && !HasCustomTags())
            {
                ClearSearch();
                return;
            }

            if (_searchPreset == SpecialSearchType.SubSearchesOnly) _searchPreset = SpecialSearchType.Nothing;
            LastSearchFilter.PageIndex = 0;
            SetSearch(LastSearchFilter).ForgetTaskSafely();
        }

        public bool HasCustomSearch() => HasCustomSearchOrFiltering();

        public bool HasCustomTags()
        {
            int tagCount = LastSearchFilter.TagAndCategoryCount;
            
            if(tagCount == 0) return false;
            
            SearchFilter prev = _resetToSearch.searchFilter;
            
            if(prev == null) return true;
            
            return prev.TagAndCategoryCount != tagCount;
        }
        
        public bool HasCustomSearchOrFiltering()
        {
            SearchFilter prev = _resetToSearch.searchFilter;
            
            if(prev == null) return false;

            return _resetToSearch.specialSearchType != _searchPreset ||
                   prev.TagAndCategoryCount != LastSearchFilter.TagAndCategoryCount ||
                   LastSearchFilter.GetUsers().Count > 0 ||
                   LastSearchFilter.GetSearchPhrase(Filtering.Like).Count > 0 ||
                   _searchPreset == SpecialSearchType.SearchForTag ||
                   _searchPreset == SpecialSearchType.SearchForUser;
        }

        public void ClearSearch()
        {
            if (_resetToSearch.searchFilter != null)
            {
                SearchFilter searchFilter = _resetToSearch.searchFilter.Clone();
                searchFilter.AddCollectionCategory(null);

                searchFilter.PageIndex = 0;
                SetSearch(searchFilter, _resetToSearch.specialSearchType, settingsFrom:LastSearchSettingsFrom);
            }
            else
            {
                Debug.LogWarning("No default search available to reset back to");
            }
        }

        public void SetSearchForUser(UserProfile user)
        {
            SearchFilter searchFilter;

            if (_searchForUser != null)
            {
                searchFilter = _searchForUser.GetSearchFilter(_defaultPageSize);
            }
            else
            {
                searchFilter = new SearchFilter(0, _defaultPageSize) { 
                    RevenueType = LastSearchFilter.RevenueType,
                    ShowMatureContent = LastSearchFilter.ShowMatureContent,
                };
            }

            searchFilter.AddUser(user);
            var specialSearchType = SpecialSearchType.SearchForUser;
            if (LastSearchPreset == SpecialSearchType.SearchCollections)
                specialSearchType = LastSearchPreset;
            SetSearch(searchFilter, specialSearchType, settingsFrom: LastSearchSettingsFrom);
        }

        public void SetSearchForTag(ModTag tag)
        {
            SearchFilter searchFilter;

            if (tag.TagType is ResourceTagType.CollectionTag or ResourceTagType.CollectionCategory)
            {
                searchFilter = new SearchFilter(0, _defaultPageSize) {
                    RevenueType = LastSearchFilter.RevenueType,
                    ShowMatureContent = LastSearchFilter.ShowMatureContent,
                };

                if (tag.TagType is ResourceTagType.CollectionCategory)
                    searchFilter.AddCollectionCategory(tag.ApiName);
                else
                    searchFilter.AddTag(tag);

                SetSearch(searchFilter, SpecialSearchType.SearchCollections);
                return;
            }

            if (_searchForTag != null)
            {
                searchFilter = _searchForTag.GetSearchFilter(_defaultPageSize);
            }
            else
            {
                var filterType = Filtering.Like;
                LastSearchFilter.ClearSearchPhrases(filterType);
                ApplyTagsToSearch(new []{tag,});
                return;
                searchFilter = new SearchFilter(0, _defaultPageSize) {
                    RevenueType = LastSearchFilter.RevenueType,
                    ShowMatureContent = LastSearchFilter.ShowMatureContent,
                };
            }

            searchFilter.AddTag(tag);
            SetSearch(searchFilter, SpecialSearchType.SearchForTag);
        }

        public void GetNextPageAdditivelyForLastSearch()
        {
            LastSearchFilter.PageIndex = _lastPageIndex + 1;

            //See if we already have more results cached
            if (_lastLocalQueryInFull != null)
            {
                LastSearchSelectionIndex = LastSearchResultMods.Count;

                int totalResults = Mathf.Min(_lastLocalQueryInFull.Count, LastSearchResultMods.Count + LastSearchFilter.PageSize);
                LastSearchResultMods = _lastLocalQueryInFull
                                       .Take(totalResults)
                                       .ToList();

                IsAdditiveSearch = true;
                
                OnSearchUpdatedUnityEvent.Invoke();
                return;
            }
            
            SetSearch(LastSearchFilter, true).ForgetTaskSafely();
        }

        public void SetPageForCurrentSearch(int page)
        {
            LastSearchFilter.PageIndex = page;
            SetSearch(LastSearchFilter).ForgetTaskSafely();
        }

        public void SetSearch(
            SearchFilter searchFilter,
            SpecialSearchType specialSearchType,
            bool resetToThis = false,
            object shareFiltersWith = null,
            ModioUISearchSettings settingsFrom = null
        ) {
            if (resetToThis) _resetToSearch = (searchFilter.Clone(), specialSearchType, shareFiltersWith);
            LastSearchSettingsFrom = settingsFrom;

            if (!ModioClient.IsInitialized || (!_allowSearchWithoutUser && (User.Current == null || !User.Current.IsInitialized)))
            {
                if (resetToThis)
                    ModioLog.Verbose?.Log(
                        "Attempting to set search before plugin is ready. Search will run once plugin is ready"
                    );
                else
                    ModioLog.Warning?.Log(
                        "Attempting to set search before plugin is ready. As resetToThis is false, this search will be discarded"
                    );

                return;
            }

            _searchPreset = specialSearchType;
            SortByOverriden = false;

            if (shareFiltersWith != null && shareFiltersWith == _shareFiltersWith)
            {
                searchFilter.AddTags(LastSearchFilter.GetTags().Where(t => !searchFilter.GetTags().Contains(t)));
                for(var f = Filtering.None; f <= Filtering.BitwiseAnd; f++)
                    searchFilter.AddSearchPhrases(LastSearchFilter.GetSearchPhrase(f), f);
            }

            _shareFiltersWith = shareFiltersWith;
            
            bool showMonetizationUI = ModioClient.Settings.TryGetPlatformSettings(out MonetizationSettings _);
            if (!showMonetizationUI) searchFilter.RevenueType = RevenueType.Free;

            if (settingsFrom != null && _isDefault)
            {
                ApplyHiddenTags().ForgetTaskSafely();
            }

            SetSearch(searchFilter).ForgetTaskSafely();

            AppliedSearchPreset?.Invoke();
        }

        async Task ApplyHiddenTags()
        {
            (Error error, GameTagCategory[] gameTagCategories) = await GameTagCategory.GetGameTagOptions();

            if (LastSearchSettingsFrom == null || gameTagCategories == null) return;
            
            foreach (GameTagCategory gameTagCategory in gameTagCategories)
            {
                bool hide = LastSearchSettingsFrom.hideTagCategories.Contains(gameTagCategory.Name);

                gameTagCategory.TempHidden = hide;

                foreach (ModTag tag in gameTagCategory.Tags)
                {
                    tag.TempHidden = hide;
                }
            }
            
            OnSearchUpdatedUnityEvent.Invoke();
        }

        public void SetCustomSearchBase(SearchFilter searchFilter, SpecialSearchType searchType)
        {
            _baseForCustomSearch = (searchFilter, searchType);
        }

        async Task SetSearch(
            SearchFilter searchFilter,
            bool isAdditiveSearch = false,
            Task<(Error error, IReadOnlyList<Mod> mods, int totalCount)> customResultProvider = null
        ) {
            LastSearchFilter = searchFilter;
            _lastPageIndex = LastSearchFilter.PageIndex;
            _lastLocalQueryInFull = null;

            IsSearching = true;
            IsAdditiveSearch = isAdditiveSearch;
            if (!isAdditiveSearch) LastSearchResultMods = Array.Empty<Mod>();
            LastSearchError = Error.None;

            OnSearchUpdatedUnityEvent.Invoke();

            var asyncSearchIndex = ++_asyncSearchIndex;

            (Error error, IReadOnlyList<Mod> mods, int totalCount) queryResultAnd;

            if (customResultProvider != null)
            {
                queryResultAnd = await customResultProvider;
            }
            else
            {
                switch (_searchPreset)
                {
                    case SpecialSearchType.Installed:
                    case SpecialSearchType.InstalledOrSubscribed:
                    case SpecialSearchType.Subscribed:
                    case SpecialSearchType.Purchased:
                        queryResultAnd = await GetModsViaLocalQuery();
                        break;
                    case SpecialSearchType.UserCreations:
                        queryResultAnd = await GetCurrentUserCreationsQuery();
                        break;
                    case SpecialSearchType.SearchCollections:
                        await SetSearchForCollections(searchFilter, isAdditiveSearch);
                        return;
                    case SpecialSearchType.SubSearchesOnly:
                        queryResultAnd = (Error.None, Array.Empty<Mod>(), 0);
                        break;
                    case SpecialSearchType.FollowedCollections:
                        await GetFollowCollectionsViaLocalQuery();
                        return;
                    case SpecialSearchType.SearchModsInCollection:
                        queryResultAnd = await GetModsInCollection(searchFilter, LastSearchSettingsFrom.CollectionId);
                        break;
                    default:
                        queryResultAnd = await GetModsViaStandardQuery();
                        break;
                }
            }
            
            if (asyncSearchIndex != _asyncSearchIndex)
            {
                // A newer search is in progress or has completed; do not apply the results of the first search
                // (particularly possible when swapping from an async search to a sync search)
                return;
            }

            IsSearching = false;

            LastSearchResultModCollections = Array.Empty<ModCollection>();
            
            if (!isAdditiveSearch)
            {
                LastSearchResultMods = queryResultAnd.mods ?? Array.Empty<Mod>();
                LastSearchSelectionIndex = 0;
            }
            else
            {
                LastSearchSelectionIndex = LastSearchResultMods.Count;
                var combinedResults = new List<Mod>(LastSearchResultMods);
                if (queryResultAnd.mods != null) combinedResults.AddRange(queryResultAnd.mods);
                LastSearchResultMods = combinedResults;
            }

            LastSearchResultTotalCount = queryResultAnd.totalCount;
            LastSearchError = queryResultAnd.error;

            if(queryResultAnd.error.Code == ErrorCode.SHUTTING_DOWN) return;
            
            OnSearchUpdatedUnityEvent.Invoke();
        }

        async Task GetFollowCollectionsViaLocalQuery()
        {
            var repo = User.Current.ModCollectionRepository;

            var collections = repo.GetFollowed().ToList();

            IsSearching = false;

            LastSearchResultModCollections = collections;
            LastSearchResultTotalCount = collections.Count;
            OnSearchUpdatedUnityEvent.Invoke();
        }

        async Task SetSearchForCollections(
            SearchFilter searchFilter,
            bool isAdditiveSearch = false
        ) {
            LastSearchFilter = searchFilter;
            _lastPageIndex = LastSearchFilter.PageIndex;
            _lastLocalQueryInFull = null;

            IsSearching = true;
            IsAdditiveSearch = isAdditiveSearch;
            if (!isAdditiveSearch) LastSearchResultMods = Array.Empty<Mod>();
            LastSearchError = Error.None;

            OnSearchUpdatedUnityEvent.Invoke();

            var asyncSearchIndex = ++_asyncSearchIndex;

            (Error error, IReadOnlyList<ModCollection> collections, int totalCount) queryResultAnd;

            queryResultAnd = await GetCollectionsViaStandardQuery();
            
            if (asyncSearchIndex != _asyncSearchIndex)
            {
                // A newer search is in progress or has completed; do not apply the results of the first search
                // (particularly possible when swapping from an async search to a sync search)
                return;
            }

            IsSearching = false;

            LastSearchResultMods = Array.Empty<Mod>();
            
            if (!isAdditiveSearch)
            {
                LastSearchResultModCollections = queryResultAnd.collections ?? Array.Empty<ModCollection>();
                LastSearchSelectionIndex = 0;
            }
            else
            {
                LastSearchSelectionIndex = LastSearchResultMods.Count;
                var combinedResults = new List<ModCollection>(LastSearchResultModCollections);
                if (queryResultAnd.collections != null) combinedResults.AddRange(queryResultAnd.collections);
                LastSearchResultModCollections = combinedResults;
            }

            LastSearchResultTotalCount = queryResultAnd.totalCount;
            LastSearchError = queryResultAnd.error;

            if(queryResultAnd.error.Code == ErrorCode.SHUTTING_DOWN) return;
            
            OnSearchUpdatedUnityEvent.Invoke();
        }

        async Task<(Error error, IReadOnlyList<Mod> mods, int totalCount)> GetModsViaStandardQuery()
        {
            ModioAPI.Mods.GetModsFilter yeet = LastSearchFilter.GetModsFilter();

            (Error error, ModioPage<Mod> page) = await Mod.GetMods(yeet);
                
            if (error)
            {
                if(!error.IsSilent)
                    ModioLog.Error?.Log($"Error getting mods: {error.GetMessage()}");
                return (error, null, 0);
            }

            return (error, page.Data, (int)page.TotalSearchResults);
        }
        async Task<(Error error, IReadOnlyList<ModCollection> mods, int totalCount)> GetCollectionsViaStandardQuery()
        {
            ModioAPI.Mods.GetModsFilter yeet = LastSearchFilter.GetModsFilter();

            ModioAPI.Collections.GetModCollectionsFilter collectionsYeet = ModioAPI.Collections.FilterGetModCollections(yeet);
            
            (Error error, ModioPage<ModCollection> page) = await ModCollection.GetCollections(collectionsYeet);
                
            if (error)
            {
                if(!error.IsSilent)
                    ModioLog.Error?.Log($"Error getting mods: {error.GetMessage()}");
                return (error, null, 0);
            }

            return (error, page.Data, (int)page.TotalSearchResults);
        }

        async Task<(Error error, IReadOnlyList<Mod> mods, int totalCount)> GetModsInCollection(SearchFilter searchFilter, long collectionId)
        {
            ModioAPI.Collections.GetCollectionModsFilter filter = ModioAPI.Collections.FilterGetCollectionMods(searchFilter.GetModsFilter());

            (Error error, ModioPage<Mod> page) = await ModCollection.GetCollectionMods(collectionId, filter);
            
            if (error)
            {
                if(!error.IsSilent)
                    ModioLog.Error?.Log($"Error getting mods: {error.GetMessage()}");
                return (error, null, 0);
            }

            return (error, page.Data, (int)page.TotalSearchResults);
        }

        async Task<(Error error, IReadOnlyList<Mod> mods, int totalCount)> GetCurrentUserCreationsQuery()
        {
            ModioAPI.Mods.GetModsFilter yeet = LastSearchFilter.GetModsFilter();

            (Error error, ModioPage<Mod> page) = await User.Current.GetUserCreationsPaged(yeet);
            
            if (error)
            {
                if(!error.IsSilent)
                    ModioLog.Error?.Log($"Error getting mods: {error.GetMessage()}");
                return (error, null, 0);
            }

            return (error, page.Data, (int)page.TotalSearchResults);
        }

        Task<(Error error, IReadOnlyList<Mod> mods, int totalCount)> GetModsViaLocalQuery()
        {
            var repo = User.Current.ModRepository;
            
            IEnumerable<Mod> mods = Enumerable.Empty<Mod>();

            if (_searchPreset == SpecialSearchType.Subscribed ||
                _searchPreset == SpecialSearchType.InstalledOrSubscribed)
            {
                mods = repo.GetSubscribed();
            }

            if (_searchPreset == SpecialSearchType.Installed ||
                _searchPreset == SpecialSearchType.InstalledOrSubscribed)
            {
                ICollection<Mod> allInstalledModIds = ModInstallationManagement.GetAllInstalledMods();
                
                if (mods == null)
                    mods = allInstalledModIds;
                else if (allInstalledModIds != null) mods = mods.Concat(allInstalledModIds);
            }

            if (_searchPreset == SpecialSearchType.Purchased)
            {
                var purchasedMods = repo.GetPurchased();

                if (mods == null)
                    mods = purchasedMods;
                else if (purchasedMods != null) mods = mods.Concat(purchasedMods);
            }

            if (mods == null)
            {
                ModioLog.Error?.Log($"Unable to construct local query results for " + _searchPreset);
                Error error = Error.Unknown;
                return Task.FromResult((error, (IReadOnlyList<Mod>)null, 0));
            }

            var modList = mods.Where(MatchesFilter).Distinct().ToList();

            modList.Sort(SortModComparer);

            var totalResultCount = modList.Count;

            if (totalResultCount > LastSearchFilter.PageSize)
            {
                _lastLocalQueryInFull = modList;
                modList = modList.Skip(LastSearchFilter.PageSize * LastSearchFilter.PageIndex)
                                 .Take(LastSearchFilter.PageSize)
                                 .ToList();
            }

            return Task.FromResult((Error.None, (IReadOnlyList<Mod>)modList, totalResultCount));
        }

        bool MatchesFilter(Mod mod)
        {
            foreach (var tag in LastSearchFilter.GetTags())
            {
                if (mod.Tags.All(modTag => modTag != tag)) 
                    return false;
            }

            foreach (var searchPhrase in LastSearchFilter.GetSearchPhrase(Filtering.Like))
            {
                //Essentially !contains, but with an invariant, case insensitive culture
                if (mod.Name.IndexOf(searchPhrase, StringComparison.InvariantCultureIgnoreCase) < 0) return false;
            }

            return true;
        }

        int SortModComparer(Mod x, Mod y)
        {
            var comparison = LastSearchFilter.SortBy switch
            {
                SortModsBy.Name          => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase),
                SortModsBy.Price         => -x.Price.CompareTo(y.Price),
                SortModsBy.Rating        => -x.Stats.RatingsPercent.CompareTo(y.Stats.RatingsPercent),
                SortModsBy.Popular       => -x.Stats.RatingsPositive.CompareTo(y.Stats.RatingsPositive),
                SortModsBy.Downloads     => -x.Stats.Downloads.CompareTo(y.Stats.Downloads),
                SortModsBy.Subscribers   => -x.Stats.Subscribers.CompareTo(y.Stats.Subscribers),
                SortModsBy.DateSubmitted => -x.DateLive.CompareTo(y.DateLive),
                _                        => throw new ArgumentOutOfRangeException(),
            };

            // (I believe some categories treat it differently)
            if (LastSearchFilter.IsSortAscending)
                comparison = -comparison;

            return comparison;
        }

        public void SetSearchForDependencies(Mod dependant)
        {
            SetSearch(new SearchFilter(), customResultProvider: GetModsViaDependencies()).ForgetTaskSafely();
            return;

            async Task<(Error error, IReadOnlyList<Mod> dependencies, int totalCount)> GetModsViaDependencies()
            {
                if (!dependant.Dependencies.HasDependencies) 
                    return (Error.None, Array.Empty<Mod>(), 0);

                (Error error, IReadOnlyList<Mod> dependencies) = await dependant.Dependencies.GetAllDependencies();

                if (error) return (error, Array.Empty<Mod>(), 0);

                return (error, dependencies, dependencies.Count);
            }
        }

        public void SetSearchForCollectionMods(ModCollection collection)
        {
            //TODO: we have a paged version of GetCollectionMods which could improve performance a decent bit
            SetSearch(new SearchFilter(), customResultProvider: GetModsViaCollection()).ForgetTaskSafely();
            return;

            async Task<(Error error, IReadOnlyList<Mod> mods, int totalCount)> GetModsViaCollection()
            {
                (Error error, IReadOnlyList<Mod> results) = await collection.GetMods();
                
                if (error) return (error, Array.Empty<Mod>(), 0);
                
                return (error, results, results.Count);
            }
        }
    }
}
