using System.Collections.Generic;
using ModIO.Implementation;

namespace ModIO
{
    /// <summary>
    /// Used to build a filter that is sent with requests for retrieving mods.
    /// </summary>
    public class SearchFilter
    {
        bool hasPageIndexBeenSet = false;
        bool hasPageSizeBeenSet = false;

#region Endpoint Parameters
        internal string sortFieldName = string.Empty;
        internal bool isSortAscending = true;
        internal SortModsBy sortBy = SortModsBy.DateSubmitted;
        internal int pageIndex;
        internal int pageSize;
        internal List<string> searchPhrases = new List<string>();
        internal List<string> tags = new List<string>();
#endregion

        public void AddSearchPhrase(string phrase)
        {
            searchPhrases.Add(phrase);
        }

        public void AddTag(string tag)
        {
            tags.Add(tag);
        }

        public void SortBy(SortModsBy category)
        {
            sortBy = category;
        }

        public void SetToAscending(bool isAscending)
        {
            isSortAscending = isAscending;
        }

        public void SetPageIndex(int pageIndex)
        {
            this.pageIndex = pageIndex;
            hasPageIndexBeenSet = true;
        }

        public void SetPageSize(int pageSize)
        {
            this.pageSize = pageSize;
            hasPageSizeBeenSet = true;
        }

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
                        + "use SetPage(int) and SetPageSize(int) before using a filter.");
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
