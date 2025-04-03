using System.Collections.Generic;
using Modio.Mods;
using UnityEngine;

namespace Modio.Unity.UI.Search
{
    [System.Serializable]
    public enum SpecialSearchType
    {
        Nothing = 8,

        Installed             = 5,
        Subscribed            = 6,
        InstalledOrSubscribed = 7,
        UserCreations         = 9,
        Purchased             = 10,
        SearchForTag,
        SearchForUser,
    }

    public class ModioUISearchSettings : MonoBehaviour
    {
        public string DisplayAs;
        public string DisplayAsLocalisedKey;
        public Sprite Icon;
        public bool HiddenIfMonetizationDisabled;

        public SpecialSearchType searchType = SpecialSearchType.Nothing;
        public string searchPhrase;
        public List<string> searchTags;
        public SortModsBy sortModsBy;
        public bool showMatureContent;
        public bool isAscending;
        public RevenueType filterRevenueType = Modio.Mods.RevenueType.Free;

        public Object shareFilterSettingsWith;

        public ModSearchFilter GetSearchFilter(int paginationSize)
        {
            var filter = new ModSearchFilter(0, paginationSize) { 
                SortBy = sortModsBy,
                ShowMatureContent = (showMatureContent),
                IsSortAscending = (isAscending),
                RevenueType = filterRevenueType, };

            filter.AddTags(searchTags);
            filter.AddSearchPhrase(searchPhrase);

            return filter;
        }

        public void Search(ModioUISearch searchWith)
        {
            if (searchWith == null) searchWith = ModioUISearch.Default;

            var searchFilter = GetSearchFilter(searchWith.DefaultPageSize);
            searchWith.SetSearch(searchFilter, searchType, true, shareFilterSettingsWith, this);
        }

        public void SetAsCustomSearchBase(ModioUISearch searchWith)
        {
            if (searchWith == null) searchWith = ModioUISearch.Default;

            var searchFilter = GetSearchFilter(searchWith.DefaultPageSize);
            searchWith.SetCustomSearchBase(searchFilter, searchType);
        }
    }
}
