using System;
using System.Collections.Generic;
using System.Linq;
using Modio.Mods;
using Modio.Search;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Search
{
    /// <summary>
    /// A container for all the information to specify a particular search
    ///
    /// You can create a blank prefab that has this component attached, then send it to a ModioUISearch
    /// </summary>
    public class ModioUISearchSettings : MonoBehaviour, IModioSearchSettings, IModioSearchCarouselProvider
    {
        
        [Serializable]
        public class ModioUICarouselSettings
        {
            public ModioUISearchSettings Search;
            public CarouselStyle Style;
        }
        
        //Note these FormerlySerializedAs are required, despite the same name, as they're now stored as <DisplayAs>k__BackingField
        [field:SerializeField, FormerlySerializedAs("DisplayAs"),]
        public string DisplayAs { get; private set; }
        [field:SerializeField, FormerlySerializedAs("DisplayAsLocalisedKey"),]
        public string DisplayAsLocalisedKey { get; private set; }
        public Sprite Icon;
        [field:SerializeField, FormerlySerializedAs("HiddenIfMonetizationDisabled"),]
        public bool HiddenIfMonetizationDisabled { get; private set; }

        [field: FormerlySerializedAs("searchType"),SerializeField]
        public SpecialSearchType SearchType { get; private set; } = SpecialSearchType.Nothing;
        [field: FormerlySerializedAs("searchPhrase"), SerializeField]
        public string SearchPhrase { get; private set; }
        [field:SerializeField, FormerlySerializedAs("searchTags"),]
        public List<string> SearchTags { get; private set; }
        [field:SerializeField, FormerlySerializedAs("sortModsBy"),]
        public SortModsBy SortModsBy { get; private set; }
        [field:SerializeField, FormerlySerializedAs("CollectionId"),]
        public long CollectionId { get; private set; }

        [field:SerializeField, FormerlySerializedAs("matureContentFilter"),]
        public MatureContentFilter MatureContentFilter { get; private set; } = MatureContentFilter.ShowAll;
        [field:SerializeField, FormerlySerializedAs("isAscending"),]
        public bool IsAscending { get; private set; }
        [field:SerializeField, FormerlySerializedAs("filterRevenueType"),]
        public RevenueType FilterRevenueType { get; private set; } = Modio.Mods.RevenueType.Free;

        public Object shareFilterSettingsWith;
        public object ShareFilterSettingsWith => shareFilterSettingsWith;
        [field:SerializeField, FormerlySerializedAs("hideTagCategories"),]
        public List<string> HideTagCategories { get; private set; }

        [SerializeField, FormerlySerializedAs("Carousels"),]
        ModioUICarouselSettings[] _carousels;
        IReadOnlyCollection<(IModioSearchSettings settings, CarouselStyle style)> _overrideCarousels;
        [field:SerializeField, FormerlySerializedAs("ShowFeaturedReasonInCarousel"),]
        public bool ShowFeaturedReasonInCarousel { get; private set; }

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

        public IReadOnlyCollection<(IModioSearchSettings settings, CarouselStyle style)> Carousels
        {
            get => _overrideCarousels ?? _carousels.Select(s => ((IModioSearchSettings)s.Search, s.Style)).ToArray();
            set => _overrideCarousels = value;
        }
    }
}
