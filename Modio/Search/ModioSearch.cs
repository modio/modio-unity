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
using Modio.Users;

namespace Modio.Search
{
    [Serializable]
    public enum SpecialSearchType
    {
        Nothing = 8,

        Installed             = 5,
        Subscribed            = 6,
        InstalledOrSubscribed = 7,
        UserCreations         = 9,
        Purchased             = 10,
        SearchForTag,
        SearchForUser,
        SubSearchesOnly,
        VcPacks,
        
        SearchCollections     = 100,
        FollowedCollections   = 101,
        SearchModsInCollection,
    }
    
    /// <summary>
    /// This is a helper that allows running searches, and getting notified about changes in the search status
    ///
    /// The reference UI uses one for the main search, as well as a separate one per carousel
    /// It's also used for showing a mods dependencies and a collection's contents
    /// </summary>
    public class ModioSearch
    {
        SpecialSearchType _searchPreset;
        (ModSearchFilter searchFilter, SpecialSearchType specialSearchType) _baseForCustomSearch;
        int _lastPageIndex;
        int _asyncSearchIndex;
        object _shareFiltersWith;
        List<Mod> _lastLocalQueryInFull;
        
        public event Action OnSearchUpdated;

        public ModioSearch(int defaultPageSize = 24) => _defaultPageSize = defaultPageSize;

        public ModSearchFilter LastSearchFilter { get; private set; } = new ModSearchFilter();
        public SpecialSearchType LastSearchPreset => _searchPreset;
        public bool IsSearching { get; private set; }
        public bool IsAdditiveSearch { get; private set; }
        public IReadOnlyList<Mod> LastSearchResultMods { get; private set; } = new Collection<Mod>();
        public IReadOnlyList<ModCollection> LastSearchResultModCollections { get; private set; } = new Collection<ModCollection>();
        public int LastSearchResultTotalCount { get; private set; }
        int ResultsOnCurrentPageCount => Math.Max(LastSearchResultMods.Count, LastSearchResultModCollections.Count);
        public int LastSearchResultPageCount => (int)Math.Ceiling(
            LastSearchResultTotalCount / (float)Math.Max(LastSearchFilter.PageSize, ResultsOnCurrentPageCount)
        );
        public bool CanGetMoreResults =>
            LastSearchResultMods != null && LastSearchResultTotalCount > ResultsOnCurrentPageCount;
        public Error LastSearchError { get; private set; } = Error.None;
        public int LastSearchSelectionIndex { get; private set; }
        
        (ModSearchFilter searchFilter, SpecialSearchType specialSearchType, object shareFiltersWith) _resetToSearch;
        int _defaultPageSize;
        long _collectionId;

        /// <summary>
        /// Change the sort mode and order for an existing search
        /// </summary>
        public void ApplySortBy(SortModsBy sortModsBy, bool ascending)
        {
            LastSearchFilter.SortBy = sortModsBy;
            LastSearchFilter.IsSortAscending = ascending;

            LastSearchFilter.PageIndex = 0;
            SetSearchAsync(LastSearchFilter, _searchPreset).ForgetTaskSafely();
        }

        /// <summary>
        /// Apply a search phrase to an existing search
        /// Uses the "Like" filtering option, and replaces any other phrases
        /// </summary>
        public void ApplySearchPhrase(string query)
        {
            ModSearchFilter searchFilter = LastSearchFilter;

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
            SetSearchAsync(searchFilter, _searchPreset).ForgetTaskSafely();
        }

        /// <summary>
        /// Apply a new set of filtering tags to an existing search
        /// Clears any non hidden tags, but leaves hidden tags applied
        /// </summary>
        /// <param name="tags"></param>
        public void ApplyTagsToSearch(IEnumerable<ModTag> tags)
        {
            bool hadTags = HasCustomTags();

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
            if (hadTags && !HasCustomTags())
            {
                if (TryClearSearch())
                    return;
            }

            if (_searchPreset == SpecialSearchType.SubSearchesOnly) _searchPreset = SpecialSearchType.Nothing;
            LastSearchFilter.PageIndex = 0;
            SetSearchAsync(LastSearchFilter, _searchPreset).ForgetTaskSafely();
        }

        /// <summary>
        /// Has the last applied preset been overriden?
        /// </summary>
        public bool HasCustomSearch()
        {
            ModSearchFilter prev = _resetToSearch.searchFilter;
            
            if(prev == null) return false;

            return _resetToSearch.specialSearchType != _searchPreset
                   || prev.TagAndCategoryCount != LastSearchFilter.TagAndCategoryCount
                   || LastSearchFilter.GetUsers().Count > 0
                   || LastSearchFilter.GetSearchPhrase(Filtering.Like).Count > 0
                   || _searchPreset == SpecialSearchType.SearchForTag
                   || _searchPreset == SpecialSearchType.SearchForUser;
        }

        /// <summary>
        /// Have any tags been applied that weren't on the last preset?
        /// </summary>
        public bool HasCustomTags()
        {
            int tagCount = LastSearchFilter.TagAndCategoryCount;
            
            if(tagCount == 0) return false;
            
            ModSearchFilter prev = _resetToSearch.searchFilter;
            
            if(prev == null) return true;
            
            return prev.TagAndCategoryCount != tagCount;
        }
        
        /// <summary>
        /// Has the order been altered at all from the last preset?
        /// </summary>
        public bool HasCustomOrdering()
        {
            ModSearchFilter prev = _resetToSearch.searchFilter;
            
            if(prev == null) return false;
            
            return prev.SortBy != LastSearchFilter.SortBy || prev.IsSortAscending !=  LastSearchFilter.IsSortAscending;
        }

        /// <summary>
        /// Reset the search back to the last applied preset. Logs a warning if there's no preset to reset to
        /// </summary>
        public void ClearSearch()
        {
            if (!TryClearSearch()) 
                ModioLog.Warning?.Log("No default search available to reset back to");
        }
        
        /// <summary>
        /// Reset the search back to the last applied preset, if there was one
        /// </summary>
        /// <returns>True if a preset was reapplied </returns>
        public bool TryClearSearch()
        {
            if (_resetToSearch.searchFilter == null) return false;
            
            ModSearchFilter searchFilter = _resetToSearch.searchFilter.Clone();
            searchFilter.AddCollectionCategory(null);

            searchFilter.PageIndex = 0;
            SetSearch(searchFilter, _resetToSearch.specialSearchType);

            return true;
        }

        public void ClearCustomOrdering()
        {
            if (_resetToSearch.searchFilter != null)
                ApplySortBy(_resetToSearch.searchFilter.SortBy, _resetToSearch.searchFilter.IsSortAscending);
        }

        /// <summary>
        /// Immediately discard all previous search results
        /// </summary>
        public void ClearCurrentResultsImmediately()
        {
            LastSearchResultMods = new Collection<Mod>();
            LastSearchResultModCollections = new Collection<ModCollection>();
            OnSearchUpdated?.Invoke();
        }

        /// <summary>
        /// Start a new search for mods by the given user.
        /// If the last search was for collections, searches for collections instead
        /// </summary>
        /// <param name="user"></param>
        public void SetSearchForUser(UserProfile user)
        {
            var searchFilter = new ModSearchFilter(0, _defaultPageSize) { 
                RevenueType = LastSearchFilter.RevenueType,
                MatureContentFilter = LastSearchFilter.MatureContentFilter,
            };

            searchFilter.AddUser(user);
            var specialSearchType = SpecialSearchType.SearchForUser;
            if (_searchPreset == SpecialSearchType.SearchCollections)
                specialSearchType = _searchPreset;
            SetSearch(searchFilter, specialSearchType);
        }

        /// <summary>
        /// Start a new search for a particular tag. If it's a collection tag, will search collections
        /// This will keep any previous hidden tags applied, and otherwise uses the filtering/sorting setting of the last search
        /// </summary>
        public void SetSearchForTag(ModTag tag)
        {
            if (tag.TagType is ResourceTagType.CollectionTag or ResourceTagType.CollectionCategory)
            {
                var searchFilter = new ModSearchFilter(0, _defaultPageSize) {
                    RevenueType = LastSearchFilter.RevenueType,
                    MatureContentFilter = LastSearchFilter.MatureContentFilter,
                };

                if (tag.TagType is ResourceTagType.CollectionCategory)
                    searchFilter.AddCollectionCategory(tag.ApiName);
                else
                    searchFilter.AddTag(tag);

                SetSearch(searchFilter, SpecialSearchType.SearchCollections);
                return;
            }

            
            var filterType = Filtering.Like;
            LastSearchFilter.ClearSearchPhrases(filterType);
            ApplyTagsToSearch(new []{tag,});
        }

        /// <summary>
        /// Fetch more results for the current search and append them to the existing search
        /// </summary>
        public void GetNextPageAdditivelyForLastSearch()
        {
            LastSearchFilter.PageIndex = _lastPageIndex + 1;

            //See if we already have more results cached
            if (_lastLocalQueryInFull != null)
            {
                LastSearchSelectionIndex = LastSearchResultMods.Count;

                int totalResults = Math.Min(_lastLocalQueryInFull.Count, LastSearchResultMods.Count + LastSearchFilter.PageSize);
                LastSearchResultMods = _lastLocalQueryInFull
                                       .Take(totalResults)
                                       .ToList();

                IsAdditiveSearch = true;

                OnSearchUpdated?.Invoke();
                return;
            }
            
            SetSearchAsync(LastSearchFilter, _searchPreset, true).ForgetTaskSafely();
        }

        /// <summary>
        /// Replace the current search results with a particular page of results
        /// </summary>
        public void SetPageForCurrentSearch(int page)
        {
            LastSearchFilter.PageIndex = page;
            SetSearchAsync(LastSearchFilter, _searchPreset).ForgetTaskSafely();
        }

        /// <summary>
        /// Run a search for a particular filter
        /// </summary>
        /// <param name="searchFilter">The filtering options to apply</param>
        /// <param name="specialSearchType">If a source other than the /mods endpoint should be used. E.g. collections, subscriptions, installed mods </param>
        /// <param name="resetToThis">Should this be treated as a preset that we'll reset back to when applying new searches</param>
        /// <param name="shareFiltersWith">Pass in the same object to multiple sequential searches to keep the same filters between those searches</param>
        /// <param name="allowSearchWithoutUser">If true, the plugin won't require a user to be signed in. Not recommended in most cases. </param>
        public void SetSearch(
            ModSearchFilter searchFilter,
            SpecialSearchType specialSearchType,
            bool resetToThis = false,
            object shareFiltersWith = null,
            bool allowSearchWithoutUser = false
        ) {
            if (resetToThis) _resetToSearch = (searchFilter.Clone(), specialSearchType, shareFiltersWith);

            if (!ModioClient.IsInitialized || (!allowSearchWithoutUser && (User.Current == null || !User.Current.IsInitialized)))
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

            if (shareFiltersWith != null && shareFiltersWith == _shareFiltersWith)
            {
                searchFilter.AddTags(LastSearchFilter.GetTags().Where(t => !searchFilter.GetTags().Contains(t)));
                for(var f = Filtering.None; f <= Filtering.BitwiseAnd; f++)
                    searchFilter.AddSearchPhrases(LastSearchFilter.GetSearchPhrase(f), f);
            }

            _shareFiltersWith = shareFiltersWith;
            
            bool showMonetizationUI = ModioClient.Settings.TryGetPlatformSettings(out MonetizationSettings _);
            if (!showMonetizationUI) searchFilter.RevenueType = RevenueType.Free;


            SetSearchAsync(searchFilter, specialSearchType).ForgetTaskSafely();

        }

        public void SetCustomSearchBase(ModSearchFilter searchFilter, SpecialSearchType searchType)
        {
            _baseForCustomSearch = (searchFilter, searchType);
        }

        async Task SetSearchAsync(
            ModSearchFilter searchFilter,
            SpecialSearchType specialSearchType,
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

            OnSearchUpdated?.Invoke();

            var asyncSearchIndex = ++_asyncSearchIndex;

            (Error error, IReadOnlyList<Mod> mods, int totalCount) queryResultAnd;

            if (customResultProvider != null)
            {
                queryResultAnd = await customResultProvider;
            }
            else
            {
                switch (specialSearchType)
                {
                    case SpecialSearchType.Installed:
                    case SpecialSearchType.InstalledOrSubscribed:
                    case SpecialSearchType.Subscribed:
                    case SpecialSearchType.Purchased:
                        queryResultAnd = await GetModsViaLocalQuery(specialSearchType);
                        break;
                    case SpecialSearchType.UserCreations:
                        queryResultAnd = await GetCurrentUserCreationsQuery();
                        break;
                    case SpecialSearchType.SearchCollections:
                        await SetSearchForCollections(searchFilter, isAdditiveSearch);
                        return;
                    case SpecialSearchType.SubSearchesOnly:
                    case SpecialSearchType.VcPacks:
                        queryResultAnd = (Error.None, Array.Empty<Mod>(), 0);
                        break;
                    case SpecialSearchType.FollowedCollections:
                        await GetFollowCollectionsViaLocalQuery();
                        return;
                    case SpecialSearchType.SearchModsInCollection:
                        queryResultAnd = await GetModsInCollection(searchFilter, _collectionId);
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
            
            OnSearchUpdated?.Invoke();
        }

        Task GetFollowCollectionsViaLocalQuery()
        {
            var repo = User.Current.ModCollectionRepository;

            var collections = repo.GetFollowed().ToList();

            IsSearching = false;

            LastSearchResultModCollections = collections;
            LastSearchResultTotalCount = collections.Count;
            OnSearchUpdated?.Invoke();
            return Task.CompletedTask;
        }

        async Task SetSearchForCollections(
            ModSearchFilter searchFilter,
            bool isAdditiveSearch = false
        ) {
            LastSearchFilter = searchFilter;
            _lastPageIndex = LastSearchFilter.PageIndex;
            _lastLocalQueryInFull = null;

            IsSearching = true;
            IsAdditiveSearch = isAdditiveSearch;
            if (!isAdditiveSearch) LastSearchResultMods = Array.Empty<Mod>();
            LastSearchError = Error.None;
            
            OnSearchUpdated?.Invoke();

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
                LastSearchSelectionIndex = LastSearchResultModCollections.Count;
                var combinedResults = new List<ModCollection>(LastSearchResultModCollections);
                if (queryResultAnd.collections != null) combinedResults.AddRange(queryResultAnd.collections);
                LastSearchResultModCollections = combinedResults;
            }

            LastSearchResultTotalCount = queryResultAnd.totalCount;
            LastSearchError = queryResultAnd.error;

            if(queryResultAnd.error.Code == ErrorCode.SHUTTING_DOWN) return;

            OnSearchUpdated?.Invoke();
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

        async Task<(Error error, IReadOnlyList<Mod> mods, int totalCount)> GetModsInCollection(ModSearchFilter searchFilter, long collectionId)
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

        Task<(Error error, IReadOnlyList<Mod> mods, int totalCount)> GetModsViaLocalQuery(SpecialSearchType specialSearchType)
        {
            var repo = User.Current.ModRepository;
            
            IEnumerable<Mod> mods = Enumerable.Empty<Mod>();

            if (specialSearchType == SpecialSearchType.Subscribed ||
                specialSearchType == SpecialSearchType.InstalledOrSubscribed)
            {
                mods = repo.GetSubscribed();
            }

            if (specialSearchType == SpecialSearchType.Installed ||
                specialSearchType == SpecialSearchType.InstalledOrSubscribed)
            {
                ICollection<Mod> allInstalledModIds = ModInstallationManagement.GetAllInstalledMods();
                
                if (mods == null)
                    mods = allInstalledModIds;
                else if (allInstalledModIds != null) mods = mods.Concat(allInstalledModIds);
            }

            if (specialSearchType == SpecialSearchType.Purchased)
            {
                var purchasedMods = repo.GetPurchased();

                if (mods == null)
                    mods = purchasedMods;
                else if (purchasedMods != null) mods = mods.Concat(purchasedMods);
            }

            if (mods == null)
            {
                ModioLog.Error?.Log($"Unable to construct local query results for " + specialSearchType);
                Error error = Error.Unknown;
                return Task.FromResult((error, (IReadOnlyList<Mod>)null, 0));
            }

            var modList = mods.Where(MatchesFilter).Distinct().ToList();

            modList.Sort((Comparison<Mod>)SortModComparer);

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
                SortModsBy.DateUpdated   => -x.DateUpdated.CompareTo(y.DateUpdated),
                _                        => throw new ArgumentOutOfRangeException(),
            };

            // (I believe some categories treat it differently)
            if (LastSearchFilter.IsSortAscending)
                comparison = -comparison;

            return comparison;
        }

        public void SetSearchForDependencies(Mod dependant)
        {
            SetSearchAsync(new ModSearchFilter(), default, customResultProvider: GetModsViaDependencies()).ForgetTaskSafely();
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

        /// <summary>
        /// Search for the mods contained by a collection
        /// </summary>
        public void SetSearchForCollectionMods(ModCollection collection)
        {
            //TODO: we have a paged version of GetCollectionMods which could improve performance a decent bit
            SetSearchAsync(new ModSearchFilter(), default, customResultProvider: GetModsViaCollection()).ForgetTaskSafely();
            return;

            async Task<(Error error, IReadOnlyList<Mod> mods, int totalCount)> GetModsViaCollection()
            {
                (Error error, IReadOnlyList<Mod> results) = await collection.GetMods();
                
                if (error) return (error, Array.Empty<Mod>(), 0);
                
                return (error, results, results.Count);
            }
        }

        /// <summary>
        /// Set the collection that's being searched for
        /// Note that this is a little hacky
        /// </summary>
        //TODO: find better alternative to this
        public void SetCollection(long collectionId)
        {
            _collectionId = collectionId;
        }
    }
}
