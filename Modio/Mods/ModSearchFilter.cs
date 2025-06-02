using System;
using System.Collections.Generic;
using System.Linq;
using Modio.API;
using Modio.Users;

namespace Modio.Mods
{
    
    /// <summary>
    /// Category to be used in the ModSearchFilter for determining how mods should be filtered in a
    /// request.
    /// </summary>
    public enum SortModsBy
    {
        Name,
        Price,
        Rating,
        Popular,
        Downloads,
        Subscribers,
        DateSubmitted,
    }
    public enum RevenueType
    {
        Free = 0,
        Paid = 1,
        FreeAndPaid = 2
    }
    public enum SearchFilterPlatformStatus
    {
        None = 0,
        PendingOnly = 1,
        LiveAndPending = 2,
    }
    [Serializable]
    public class ModSearchFilter
    {
        public int PageIndex
        {
            get => _pageIndex;
            set => _pageIndex = value < 0 ? 0 : value;
        }
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 1 : value > 100 ? 100 : value;
        }
        #region Endpoint Parameters
        Dictionary<Filtering, List<string>> _searchPhrases;
        List<string> _tags;
        List<UserProfile> _users;
        
        public bool ShowMatureContent { get; set; }
        public SearchFilterPlatformStatus PlatformStatus  { get; set; } = SearchFilterPlatformStatus.None;
        
        public SortModsBy SortBy { get; set; } = SortModsBy.DateSubmitted;
        public bool IsSortAscending { get; set; } = true;

        public RevenueType RevenueType { get; set; } = RevenueType.Free;
        //public bool ShowSoldOut;
        
        int _pageSize;
        int _pageIndex;

        #endregion

        /// <param name="pageIndex">The search will skip <c>pageIndex * pageSize</c> results and return (up to) the following <see cref="PageSize"/> results.</param>
        /// <param name="pageSize">
        ///     Limit the number of results returned (100 max).
        ///     <p>Use <see cref="SetPageIndex"/> to skip results and return later results.</p>
        /// </param>
        public ModSearchFilter(int pageIndex = 0, int pageSize = 100)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
        }

        /// <summary>
        /// Adds a phrase into the filter to be used when filtering mods in a request.
        /// </summary>
        /// <param name="phrase">the string to be added to the filter</param>
        /// <param name="filtering">(Optional) type of filter to be used with the text, defaults to Full text search</param>
        public void AddSearchPhrase(string phrase, Filtering filtering = Filtering.Like)
        {
            //Don't add a search phrase if it's empty as the server will ignore it anyway
            if(string.IsNullOrEmpty(phrase))
                return;

            _searchPhrases ??= new Dictionary<Filtering, List<string>>();
            if (!_searchPhrases.TryGetValue(filtering, out List<string> value))
            {
                value = new List<string>();
                _searchPhrases.Add(filtering, value);
            }
            value.Add(phrase);
        }
        public void AddSearchPhrases(ICollection<string> phrase, Filtering filtering = Filtering.Like)
        {
            //Don't add a search phrase if it's empty as the server will ignore it anyway
            if(phrase == null || phrase.Count == 0)
                return;

            _searchPhrases ??= new Dictionary<Filtering, List<string>>();
            if (!_searchPhrases.TryGetValue(filtering, out List<string> value))
            {
                value = new List<string>();
                _searchPhrases.Add(filtering, value);
            }
            value.AddRange(phrase);
        }

        public void ClearSearchPhrases()
        {
            _searchPhrases?.Clear();
        }

        public void ClearSearchPhrases(Filtering filtering)
        {
            _searchPhrases?.Remove(filtering);
        }

        public IList<string> GetSearchPhrase(Filtering filtering)
        {
            if (_searchPhrases != null && _searchPhrases.TryGetValue(filtering, out List<string> value)) return value;
            return Array.Empty<string>();
        }

        /// <summary>
        /// Adds a tag to be used in filtering mods for a request.
        /// </summary>
        /// <param name="tag">the tag to be added to the filter</param>
        /// <seealso cref="ModTag"/>
        /// <seealso cref="GameTagCategory"/>
        public void AddTag(string tag)
        {
            _tags ??= new List<string>();
            _tags.Add(tag);
        }

        /// <summary>
        /// Adds multiple tags used in filtering mods for a request.
        /// </summary>
        /// <param name="tags">the tags to be added to the filter</param>
        /// <seealso cref="ModTag"/>
        /// <seealso cref="GameTagCategory"/>
        public void AddTags(IEnumerable<string> tags)
        {
            _tags ??= new List<string>();
            _tags.AddRange(tags);
        }

        public void ClearTags()
        {
            _tags?.Clear();
        }

        public IReadOnlyList<string> GetTags() => _tags ?? (IReadOnlyList<string>)Array.Empty<string>() ;

        /// <summary>
        /// Adds a specific user to the filter, so that mods that were not created by the user
        /// (or other users added to the filter) will not be returned.
        /// </summary>
        /// <seealso cref="UserProfile"/>
        public void AddUser(UserProfile user)
        {
            _users ??= new List<UserProfile>();
            _users.Add(user);
        }

        public IReadOnlyList<UserProfile> GetUsers() => _users ?? (IReadOnlyList<UserProfile>)Array.Empty<UserProfile>();
        
        public ModioAPI.Mods.GetModsFilter GetModsFilter()
        {
            var filter = ModioAPI.Mods.FilterGetMods(PageIndex, PageSize);
            
            for (var filtering = Filtering.None; filtering <= Filtering.BitwiseAnd; filtering++)
            {
                IList<string> searchPhrases = GetSearchPhrase(filtering);
                if (searchPhrases.Count > 0) filter.Name($"*{searchPhrases[0]}*", filtering);
            }
            
            if (_tags != null && _tags.Count > 0) filter.Tags(_tags);
            
            if(_users != null && _users.Count > 0) filter.SubmittedBy(_users.Select(u => u.UserId).ToArray());

            filter.MaturityOption(ShowMatureContent ? 0b1111 : 0b0000,
                ShowMatureContent ? Filtering.BitwiseAnd : Filtering.None);

            string platformStatusFilter = PlatformStatus switch
            {
                SearchFilterPlatformStatus.None           => null,
                SearchFilterPlatformStatus.PendingOnly    => "pending_only",
                SearchFilterPlatformStatus.LiveAndPending => "live_and_pending",
                _                                         => throw new ArgumentOutOfRangeException()
            };

            if (platformStatusFilter != null) filter.PlatformStatus(platformStatusFilter);

            string sortBy = SortBy switch
            {
                SortModsBy.Name          => "name",
                SortModsBy.Price         => "price",
                SortModsBy.Rating        => "ratings_weighted_aggregate",
                SortModsBy.Popular       => "downloads_today",
                SortModsBy.Downloads     => "downloads_total",
                SortModsBy.Subscribers   => "subscribers_total",
                SortModsBy.DateSubmitted => "id",
                _                        => throw new ArgumentOutOfRangeException()
            };
            filter.SortByStringType(sortBy, IsSortAscending);

            filter.RevenueType((long)RevenueType);
            //filter.Stock(ShowSoldOut ? 0 : 1);
            
            return filter;
        }
    }
}
