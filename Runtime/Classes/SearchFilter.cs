using System.Collections.Generic;
using ModIO.Implementation;

namespace ModIO
{
    /// <summary>
    /// Used to build a filter that is sent with requests for retrieving mods.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetMods"/>
    /// <seealso cref="ModIOUnityAsync.GetMods"/>
    public class SearchFilter
    {
        bool hasPageIndexBeenSet = false;
        bool hasPageSizeBeenSet = false;

#region Endpoint Parameters
        // These are for internal use. Do not use
        internal string sortFieldName = string.Empty;
        internal bool isSortAscending = true;
        internal SortModsBy sortBy = SortModsBy.DateSubmitted;
        internal int pageIndex;
        internal int pageSize;
        internal List<string> searchPhrases = new List<string>();
        internal List<string> tags = new List<string>();
#endregion
        /// <summary>
        /// Adds a phrase into the filter to be used when filtering mods in a request.
        /// </summary>
        /// <param name="phrase">the string to be added to the filter</param>
        public void AddSearchPhrase(string phrase)
        {
            searchPhrases.Add(phrase);
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
        public void SortBy(SortModsBy category)
        {
            sortBy = category;
        }

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
        /// Sets the zero based index of the page. eg if there are 1,000 results based on the filter
        /// settings provided, and the page size is 100. Setting this to 1 will return the mods from
        /// 100-200. Whereas setting this to 0 will return the first 100 results.
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <seealso cref="SetPageSize"/>
        public void SetPageIndex(int pageIndex)
        {
            this.pageIndex = pageIndex;
            hasPageIndexBeenSet = true;
        }

        /// <summary>
        /// Sets the maximum page size of the request. eg if there are 50 results and the index is
        /// set to 0. If the page size is set to 10 you will receive the first 10 results. If the
        /// page size is set to 100 you will only receive the total 50 results, because there are
        /// no more to be got.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <seealso cref="SetPageIndex"/>
        public void SetPageSize(int pageSize)
        {
            this.pageSize = pageSize;
            hasPageSizeBeenSet = true;
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
            bool paginationSet = hasPageIndexBeenSet && hasPageSizeBeenSet;

            // TODO Check for illegal characters in search phrase?

            // TODO Check if tags are correct? Or will they just get ignored?
            // ^ Perhaps log a warning if non-fatal

            if(!paginationSet)
            {
                result = ResultBuilder.Create(ResultCode.InvalidParameter_PaginationParams);
                Logger.Log(
                    LogLevel.Error,
                    "The pagination parameters haven't been set for this filter. Make sure to "
                        + "use SetPageIndex(int) and SetPageSize(int) before using a filter.");
            }
            else
            {
                result = ResultBuilder.Success;
                return true;
            }

            return false;
        }
    }
}
