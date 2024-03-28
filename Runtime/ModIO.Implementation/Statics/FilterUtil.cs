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
            if (searchFilter == null) return url;

            string ascendingOrDescending =
                searchFilter.isSortAscending ? Filtering.Ascending : Filtering.Descending;
            // Set Filtering Order
            switch(searchFilter.sortBy)
            {
                case SortModsBy.Name:
                    url += $"&{ascendingOrDescending}name";
                    break;
                case SortModsBy.Price:
                    url += $"&{ascendingOrDescending}price";
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

            if (searchFilter.searchPhrases != null)
            {
                // Add Search Phrases
                foreach(var s in searchFilter.searchPhrases.Values)
                {
                    url += s;
                }
            }

            // add tags to filter
            if(searchFilter.tags != null && searchFilter.tags.Count > 0)
            {
                url += "&tags=";
                foreach(string tag in searchFilter.tags)
                {
                    url += $"{tag},";
                }
                url = url.Trim(',');
            }
            // add users we are looking for
            if(searchFilter.users != null && searchFilter.users.Count > 0)
            {
                url += "&submitted_by=";
                foreach(long user in searchFilter.users)
                {
                    url += $"{user},";
                }
                url = url.Trim(',');
            }
            if(!searchFilter.showMatureContent)
            {
                url += "&maturity_option=0";
            }

            // marketplace revenue type
            if(searchFilter.revenueType != RevenueType.Free)
            {
                url += $"&revenue_type={(int)searchFilter.revenueType}";
            }

            return url;
        }

        public static string AddPagination(SearchFilter filter, string url)
        {
            // Set Pagination
            int limit = 100;
            int offset = filter.pageIndex * filter.pageSize;

            url += $"&{Filtering.Limit}{limit}&{Filtering.Offset}{offset}";

            return url;
        }

        public static string LastEntryPagination()
        {
            return $"&{Filtering.Descending}id&{Filtering.Limit}{1}&{Filtering.Offset}{0}";
        }
    }
}
