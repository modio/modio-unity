using System;
using System.Collections.Generic;
using System.Linq;
using Modio.Mods;

namespace Modio.Search
{
    public class FeaturedContentSearchSettings : IModioSearchSettings
    {
        public string DisplayAs { get; }
        public string DisplayAsLocalisedKey { get; }
        public bool HiddenIfMonetizationDisabled => false;
        public SpecialSearchType SearchType { get; }
        public string SearchPhrase => null;
        public List<string> SearchTags { get; }
        public SortModsBy SortModsBy { get; }
        public long CollectionId => 0;
        public MatureContentFilter MatureContentFilter { get; }
        public bool IsAscending { get; }
        public RevenueType FilterRevenueType { get; }
        public List<string> HideTagCategories => null;
        public bool ShowFeaturedReasonInCarousel => false;
        public object ShareFilterSettingsWith => null;

        public CarouselStyle Style { get; }

        int _overridePageSize;
        Dictionary<string, string> _unknownFilters;

        public FeaturedContentSearchSettings(ModioFeaturedContent featuredContent)
        {
            DisplayAs = featuredContent.LocalizedName;
            SearchType = SpecialSearchType.Nothing;

            _overridePageSize = featuredContent.Total;
            
            Style = featuredContent.Size switch
            {
                "small"  => CarouselStyle.Default,
                "medium" => CarouselStyle.Featured,
                "large"  => CarouselStyle.FeaturedLarge,
                _        => CarouselStyle.Default,
            };

            MatureContentFilter = MatureContentFilter.ShowAll;
            
            foreach ((string key, string value) in featuredContent.Filters)
            {
                switch (key)
                {
                    case "tags":
                        SearchTags = value.Split(',').ToList();
                        break;
                    case "_sort" when TryParseSortBy(value, out bool isAscending, out SortModsBy sortModsBy):
                        IsAscending = isAscending;
                        SortModsBy = sortModsBy;
                        break;
                    case "revenue_type" when int.TryParse(value, out int revenueType):
                        FilterRevenueType = (RevenueType)revenueType;
                        break;
                    case "mature_content" when int.TryParse(value, out int matureContentType):
                        MatureContentFilter = (MatureContentFilter)matureContentType;
                        break;
                    //TODO: Potentially add support to rest of plugin if desired. For now, suppress warning (these are known things we don't support)
                    // We still get the intended experience if a user clicks "view more"
                    case "submitted_by_display_name":
                    case "timespan":
                        _unknownFilters ??= new Dictionary<string, string>();
                        _unknownFilters[key] = value;
                        break;
                    default:
                        ModioLog.Warning?.Log($"Unknown or unprocessable search filter on placement: '{key}', '{value}'");
                        _unknownFilters ??= new Dictionary<string, string>();
                        _unknownFilters[key] = value;
                        break;
                }
            }

        }

        bool TryParseSortBy(string value, out bool isAscending, out SortModsBy sortModsBy)
        {
            isAscending = value.StartsWith("-");
            value = isAscending ? value[1..] : value;

            SortModsBy invalid = (SortModsBy)-1;

            sortModsBy = value switch
            {
                "name"                       => SortModsBy.Name,
                "price"                      => SortModsBy.Price,
                "ratings_weighted_aggregate" => SortModsBy.Rating,
                "downloads_today"            => SortModsBy.Popular,
                "downloads_total"            => SortModsBy.Downloads,
                "subscribers_total"          => SortModsBy.Subscribers,
                "id"                         => SortModsBy.DateSubmitted,
                "date_updated"               => SortModsBy.DateUpdated,
                "date_live"                  => SortModsBy.DateLive,
                _                            => invalid,
            };

            return sortModsBy != invalid;
        }

        public ModSearchFilter GetSearchFilter(int paginationSize)
        {
            //Begin common with ModioUISearchSettings
            var filter = new ModSearchFilter(0, paginationSize) { 
                SortBy = SortModsBy,
                MatureContentFilter = (MatureContentFilter),
                IsSortAscending = (IsAscending),
                RevenueType = FilterRevenueType, };

            if (SearchTags != null)
                filter.AddTags(
                    SearchTags,
                    SearchType == SpecialSearchType.SearchCollections
                        ? ResourceTagType.CollectionTag
                        : ResourceTagType.ModTag
                );

            filter.AddSearchPhrase(SearchPhrase);
            //End common with ModioUISearchSettings

            if (_unknownFilters != null)
                filter.SetUnknownFilters(_unknownFilters);

            if(_overridePageSize > 0)
                filter.PageSize = _overridePageSize;
            
            return filter;
        }
    }
}
