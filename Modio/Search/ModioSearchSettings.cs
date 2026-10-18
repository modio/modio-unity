using System.Collections.Generic;
using Modio.Mods;

namespace Modio.Search
{
    /// <summary>
    /// A raw C# class that can be used to start a search
    /// For a Unity serialized version, see ModioUISearchSettings
    /// </summary>
    public class ModioSearchSettings : IModioSearchSettings, IModioSearchCarouselProvider
    {
        public string DisplayAs { get; set; }
        public string DisplayAsLocalisedKey { get; set; }
        public bool HiddenIfMonetizationDisabled { get; set; }
        public SpecialSearchType SearchType { get; set; } = SpecialSearchType.Nothing;
        public string SearchPhrase { get; set; }
        public List<string> SearchTags { get; set; }
        public SortModsBy SortModsBy { get; set; }
        public long CollectionId { get; set; }
        public MatureContentFilter MatureContentFilter { get; set; } = MatureContentFilter.ShowAll;
        public bool IsAscending { get; set; }
        public RevenueType FilterRevenueType { get; set; } = Modio.Mods.RevenueType.FreeAndPaid;
        public List<string> HideTagCategories { get; set; }
        public bool ShowFeaturedReasonInCarousel { get; set; }
        public object ShareFilterSettingsWith { get; set; }
        
        public IReadOnlyCollection<(IModioSearchSettings settings, CarouselStyle style)> Carousels { get; set; }

        public ModSearchFilter GetSearchFilter(int paginationSize)
        {
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

            return filter;
        }
    }
}
