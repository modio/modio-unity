using System.Collections.Generic;
using Modio.Mods;

namespace Modio.Search
{
    public interface IModioSearchSettings
    {
        public string DisplayAs { get; }
        public string DisplayAsLocalisedKey { get; }
        public bool HiddenIfMonetizationDisabled { get; }
        public SpecialSearchType SearchType  { get; }
        public string SearchPhrase { get; }
        public List<string> SearchTags { get; }
        public SortModsBy SortModsBy { get; }
        public long CollectionId { get; }
        public MatureContentFilter MatureContentFilter { get; }
        public bool IsAscending { get; }
        public RevenueType FilterRevenueType  { get; }
        public List<string> HideTagCategories { get; }
        public bool ShowFeaturedReasonInCarousel { get; }
        public object ShareFilterSettingsWith { get; }
        
        ModSearchFilter GetSearchFilter(int paginationSize);
    }
}
