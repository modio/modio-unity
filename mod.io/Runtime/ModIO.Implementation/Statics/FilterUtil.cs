using System;
using ModIO.Implementation.API.Requests;

namespace ModIO.Implementation
{
    /// <summary>
    /// Filter Utility methods
    /// </summary>    
    internal static class FilterUtil
    {
        public static string ConvertToURL(SearchFilter searchFilter)
        {
            // TODO change this to a StringBuilder
            string url = string.Empty;
            string ascendingOrDescending =
                searchFilter.isSortAscending ? Filtering.Ascending : Filtering.Descending;
            // Set Filtering Order
            switch(searchFilter.sortBy)
            {
                case SortModsBy.Name:
                    url += $"&{ascendingOrDescending}name";
                    break;
                case SortModsBy.Rating:
                    url += $"&{ascendingOrDescending}rating";
                    break;
                case SortModsBy.Popular:
                    url += $"&{ascendingOrDescending}popular";
                    break;
                case SortModsBy.Downloads:
                    url += $"&{ascendingOrDescending}downloads";
                    break;
                case SortModsBy.Subscribers:
                    url += $"&{ascendingOrDescending}subscribers";
                    break;
                case SortModsBy.DateSubmitted:
                    url += $"&{ascendingOrDescending}id";
                    break;
            }
            // Add Search Phrases
            foreach(string phrase in searchFilter.searchPhrases)
            {
                if(!string.IsNullOrWhiteSpace(phrase))
                {
                    url += $"&{Filtering.FullTextSearch}{phrase}";
                }
            }
            // add tags to filter
            if(searchFilter.tags.Count > 0)
            {
                url += "&tags=";
                foreach(string tag in searchFilter.tags)
                {
                    url += $"{tag},";
                }
                url = url.Trim(',');
            }

            return url;
        }

        public static string AddPagination(SearchFilter filter, string url)
        {
            // Set Pagination
            int limit = filter.pageSize;
            int offset = 100 * filter.pageIndex; // always get 100

            url += $"&{Filtering.Limit}{limit}&{Filtering.Offset}{offset}";

            return url;
        }

        public static string LastEntryPagination()
        {
            return $"&{Filtering.Descending}id&{Filtering.Limit}{1}&{Filtering.Offset}{0}";
        }
    }
}
