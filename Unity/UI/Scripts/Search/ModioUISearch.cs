using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Modio.API;
using Modio.Errors;
using Modio.Mods;
using Modio.Unity.Settings;
using Modio.Unity.UI.Components;
using Modio.Users;
using UnityEngine;
using UnityEngine.Events;
using SearchFilter = Modio.Mods.ModSearchFilter;
using UserProfile = Modio.Users.UserProfile;

namespace Modio.Unity.UI.Search
{
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
        public int LastSearchResultModCount { get; private set; }
        public int LastSearchResultPageCount => Mathf.CeilToInt(
            LastSearchResultModCount / (float)Mathf.Max(LastSearchFilter.PageSize, LastSearchResultMods.Count)
        );
        public bool CanGetMoreMods =>
            LastSearchResultMods != null && LastSearchResultModCount > LastSearchResultMods.Count;
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
            SetSearch(LastSearchFilter);
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

            searchFilter.PageIndex = 0;
            SetSearch(searchFilter);
        }

        public void ApplyTagsToSearch(IEnumerable<string> tags)
        {
            LastSearchFilter.ClearTags();
            LastSearchFilter.AddTags(tags);

            //If we were doing a tag based search, and we just removed all tags, clear search instead
            if (_searchPreset == SpecialSearchType.SearchForTag && !LastSearchFilter.GetTags().Any())
            {
                ClearSearch();
                return;
            }
            LastSearchFilter.PageIndex = 0;
            SetSearch(LastSearchFilter);
        }

        public bool HasCustomSearch()
        {
            return LastSearchFilter.GetUsers().Count > 0 ||
                   LastSearchFilter.GetSearchPhrase(Filtering.Like).Count > 0 ||
                   _searchPreset == SpecialSearchType.SearchForTag ||
                   _searchPreset == SpecialSearchType.SearchForUser;
        }

        public void ClearSearch()
        {
            if (_resetToSearch.searchFilter != null)
            {
                SearchFilter searchFilter = _resetToSearch.searchFilter;
                searchFilter.ClearSearchPhrases(Filtering.Like);
                searchFilter.ClearTags();

                searchFilter.PageIndex = 0;
                SetSearch(searchFilter, _resetToSearch.specialSearchType);
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
            SetSearch(searchFilter, SpecialSearchType.SearchForUser);
        }

        public void SetSearchForTag(ModTag tag)
        {
            SearchFilter searchFilter;

            if (_searchForTag != null)
            {
                searchFilter = _searchForTag.GetSearchFilter(_defaultPageSize);
            }
            else
            {
                searchFilter = new SearchFilter(0, _defaultPageSize) {
                    RevenueType = LastSearchFilter.RevenueType,
                    ShowMatureContent = LastSearchFilter.ShowMatureContent,
                };
            }

            searchFilter.AddTag(tag.ApiName);
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

                OnSearchUpdatedUnityEvent.Invoke();
                return;
            }
            
            SetSearch(LastSearchFilter, true);
        }

        public void SetPageForCurrentSearch(int page)
        {
            LastSearchFilter.PageIndex = page;
            SetSearch(LastSearchFilter);
        }

        public void SetSearch(
            SearchFilter searchFilter,
            SpecialSearchType specialSearchType,
            bool resetToThis = false,
            object shareFiltersWith = null,
            ModioUISearchSettings settingsFrom = null
        )
        {
            if (resetToThis) _resetToSearch = (searchFilter, specialSearchType, shareFiltersWith);
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
                searchFilter.AddTags(LastSearchFilter.GetTags());
                for(var f = Filtering.None; f <= Filtering.BitwiseAnd; f++)
                    searchFilter.AddSearchPhrases(LastSearchFilter.GetSearchPhrase(f), f);
            }

            _shareFiltersWith = shareFiltersWith;

            var compUISettings = ModioClient.Settings.GetPlatformSettings<ModioComponentUISettings>();
            bool showMonetizationUI = compUISettings is { ShowMonetizationUI: true, };
            if (!showMonetizationUI)
            {
                searchFilter.RevenueType = RevenueType.Free;
            }

            SetSearch(searchFilter);

            AppliedSearchPreset?.Invoke();
        }

        public void SetCustomSearchBase(SearchFilter searchFilter, SpecialSearchType searchType)
        {
            _baseForCustomSearch = (searchFilter, searchType);
        }

        async void SetSearch(
            SearchFilter searchFilter,
            bool isAdditiveSearch = false,
            Task<(Error error, IReadOnlyList<Mod> mods, int totalCount)> customResultProvider = null
        )
        {
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

            LastSearchResultModCount = queryResultAnd.totalCount;
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

        async Task<(Error error, IReadOnlyList<Mod> mods, int totalCount)> GetModsViaLocalQuery()
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
                return (error, null, 0);
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

            return (Error.None, modList, totalResultCount);
        }

        bool MatchesFilter(Mod mod)
        {
            foreach (var tag in LastSearchFilter.GetTags())
            {
                if (mod.Tags.All(modTag => modTag.ApiName != tag)) 
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
            SetSearch(new SearchFilter(), customResultProvider: GetModsViaDependencies());
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
    }
}
