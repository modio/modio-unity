using System;
using Modio.Mods;
using Modio.Unity.UI.Components.Localization;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertySortType : ISearchProperty
    {
        [SerializeField] TMP_Text _searchText;

        [SerializeField] ModioUILocalizedText _locText;
        [SerializeField] bool _alwaysShowSortType;
        
        [SerializeField] GameObject _enableIfAscending;
        [SerializeField] GameObject _enableIfDescending;

        public void OnSearchUpdate(ModioUISearch search)
        {
            if (search.ModioSearch.HasCustomOrdering() || _alwaysShowSortType)
            {
                SortModsBy sortModsBy = search.ModioSearch.LastSearchFilter.SortBy;
                var sortByKey = GetLocalizationKey(sortModsBy);
                _locText.SetKey(sortByKey);
            }
            else
            {
                _locText.SetKey(GetLocalizationKey((SortModsBy)(-1)));
            }
            
            if(_enableIfAscending != null) _enableIfAscending.SetActive(search.ModioSearch.LastSearchFilter.IsSortAscending);
            if(_enableIfDescending != null) _enableIfDescending.SetActive(!search.ModioSearch.LastSearchFilter.IsSortAscending);
        }

        public static string GetLocalizationKey(SortModsBy sortModsBy) => sortModsBy switch
        {
            SortModsBy.Name          => "modio_sort_type_name",
            SortModsBy.Price         => "modio_sort_type_price",
            SortModsBy.Rating        => "modio_sort_type_rating",
            SortModsBy.Popular       => "modio_sort_type_popular",
            SortModsBy.Downloads     => "modio_sort_type_downloads",
            SortModsBy.Subscribers   => "modio_sort_type_subscribers",
            SortModsBy.DateSubmitted => "modio_sort_type_date_submitted",
            SortModsBy.DateUpdated   => "modio_sort_type_date_updated",
            _                        => "modio_sort_type_blank",
        };
    }
}
