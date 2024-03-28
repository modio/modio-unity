using ModIO.Implementation;
using ModIO.Implementation.API.Requests;
using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = ModIO.Implementation.Logger;

namespace ModIO
{
    /// <summary>
    /// Used to build a filter that is sent with requests for retrieving mods.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetMods"/>
    /// <seealso cref="ModIOUnityAsync.GetMods"/>
    [Serializable]
    public class SearchFilter
    {
        #region Endpoint Parameters
        // These are for internal use. Do not use
        internal string sortFieldName = string.Empty;
        [SerializeField] internal bool isSortAscending = true;
        [SerializeField] internal SortModsBy sortBy = SortModsBy.DateSubmitted;
        internal int pageIndex;
        internal int pageSize;
        internal Dictionary<FilterType, string> searchPhrases = new Dictionary<FilterType, string>();
        internal List<string> tags = new List<string>();
        internal List<long> users = new List<long>();
        internal bool showMatureContent = false;

        [SerializeField] internal RevenueType revenueType = RevenueType.Free;
        [SerializeField] internal int stock = Mods_DontShowSoldOut;

        /// <summary>
        /// The search will now show sold out mods
        /// </summary>
        public void ShowSoldOut() => stock = Mods_ShowSoldOut;
        const int Mods_ShowSoldOut = 0;

        /// <summary>
        /// The search will now not show sold out mods
        /// </summary>
        public void DontShowSoldOut() => stock = Mods_DontShowSoldOut;
        const int Mods_DontShowSoldOut = 1;
        #endregion

        /// <param name="pageIndex">The search will skip <c>pageIndex * pageSize</c> results and return (up to) the following <see cref="pageSize"/> results.</param>
        /// <param name="pageSize">
        ///     Limit the number of results returned (100 max).
        ///     <p>Use <see cref="SetPageIndex"/> to skip results and return later results.</p>
        /// </param>
        public SearchFilter(int pageIndex = 0, int pageSize = 100)
        {
            SetPageIndex(pageIndex);
            SetPageSize(pageSize);
        }

        /// <summary>
        /// Choose if the filter should include/exclude free or paid mods.
        /// </summary>
        /// <param name="type"></param>
        public RevenueType RevenueType
        {
            get => this.revenueType;
            set => revenueType = value;
        }

        public void ShowMatureContent(bool value) => showMatureContent = value;

        /// <summary>
        /// Adds a phrase into the filter to be used when filtering mods in a request.
        /// </summary>
        /// <param name="phrase">the string to be added to the filter</param>
        /// <param name="filterType">(Optional) type of filter to be used with the text, defaults to Full text search</param>
        public void AddSearchPhrase(string phrase, FilterType filterType = FilterType.FullTextSearch)
        {
            string url = string.Empty;
            switch (filterType)
            {
                case FilterType.FullTextSearch:
                    url += $"&{Filtering.FullTextSearch}{phrase}";
                    break;
                case FilterType.NotEqualTo:
                    url += $"&{Filtering.NotEqualTo}{phrase}";
                    break;
                case FilterType.Like:
                    url += $"&{Filtering.Like}{phrase}";
                    break;
                case FilterType.NotLike:
                    url += $"&{Filtering.NotLike}{phrase}";
                    break;
                case FilterType.In:
                    url += $"&{Filtering.In}{phrase}";
                    break;
                case FilterType.NotIn:
                    url += $"&{Filtering.NotIn}{phrase}";
                    break;
                case FilterType.Max:
                    url += $"&{Filtering.Max}{phrase}";
                    break;
                case FilterType.Min:
                    url += $"&{Filtering.Min}{phrase}";
                    break;
                case FilterType.BitwiseAnd:
                    url += $"&{Filtering.BitwiseAnd}{phrase}";
                    break;
                default:
                    break;
            }

            if (searchPhrases.ContainsKey(filterType))
            {
                searchPhrases[filterType] += url;
            }
            else
            {
                searchPhrases.Add(filterType, url);
            }
        }

        /// <summary>
        /// Adds a tag to be used in filtering mods for a request.
        /// </summary>
        /// <param name="tag">the tag to be added to the filter</param>
        /// <seealso cref="Tag"/>
        /// <seealso cref="TagCategory"/>
        public void AddTag(string tag)
        {
            tags.Add(tag);
        }

        /// <summary>
        /// Determines what category mods should be sorted and returned by. eg if the category
        /// SortModsBy.Downloads was used, then the results would be returned by the number of
        /// downloads. Depending on the Ascending or Descending setting, it will start or end with
        /// mods that have the highest or lowest number of downloads.
        /// </summary>
        /// <param name="category">the category to sort the request</param>
        /// <seealso cref="SetToAscending"/>
        public void SetSortBy(SortModsBy category)
        {
            sortBy = category;
        }

        public SortModsBy SortBy => sortBy;

        /// <summary>
        /// Determines the order of the results being returned. eg should results be filtered from
        /// highest to lowest, or lowest to highest.
        /// </summary>
        /// <param name="isAscending"></param>
        public void SetToAscending(bool isAscending)
        {
            isSortAscending = isAscending;
        }

        /// <summary>
        /// The search will skip <c>pageIndex * pageSize</c> results and return (up to) the following <see cref="pageSize"/> results.
        /// </summary>
        /// <seealso cref="SetPageSize"/>
        public void SetPageIndex(int pageIndex)
        {
            this.pageIndex = pageIndex < 0 ? 0 : pageIndex;
        }

        /// <summary>
        ///     <p>Limit the number of results returned (100 max).</p>
        ///     <p>Use <see cref="SetPageIndex"/> to skip results and return later results.</p>
        /// </summary>
        /// <seealso cref="SetPageIndex"/>
        public void SetPageSize(int pageSize)
        {
            this.pageSize = pageSize < 1 ? 1 : pageSize > 100 ? 100 : pageSize;
        }

        /// <summary>
        /// Adds a specific user to the filter, so that mods that were not created by the user
        /// (or other users added to the filter) will not be returned.
        /// </summary>
        /// <param name="userId">Id of the user to add</param>
        /// <seealso cref="UserProfile"/>
        public void AddUser(long userId)
        {
            users.Add(userId);
        }

        /// <summary>
        /// You can use this method to check if a search filter is setup correctly before using it
        /// in a GetMods request.
        /// </summary>
        /// <param name="result"></param>
        /// <returns>true if the filter is valid</returns>
        /// <seealso cref="ModIOUnity.GetMods"/>
        /// <seealso cref="ModIOUnityAsync.GetMods"/>
        public bool IsSearchFilterValid(out Result result)
        {
            // TODO Check for illegal characters in search phrase?

            // TODO Check if tags are correct? Or will they just get ignored?
            // ^ Perhaps log a warning if non-fatal

            result = ResultBuilder.Success;

            return true;
        }
    }
}
