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

        public void OnSearchUpdate(ModioUISearch search)
        {
            if (search.SortByOverriden)
            {
                SortModsBy sortModsBy = search.LastSearchFilter.SortBy;
                var sortByKey = GetLocalizationKey(sortModsBy);
                _locText.SetKey(sortByKey);
            }
            else
            {
                _locText.SetKey(GetLocalizationKey((SortModsBy)(-1)));
            }
        }

        static string GetLocalizationKey(SortModsBy sortModsBy) => sortModsBy switch
        {
            SortModsBy.Name          => "modio_sort_type_name",
            SortModsBy.Price         => "modio_sort_type_price",
            SortModsBy.Rating        => "modio_sort_type_rating",
            SortModsBy.Popular       => "modio_sort_type_popular",
            SortModsBy.Downloads     => "modio_sort_type_downloads",
            SortModsBy.Subscribers   => "modio_sort_type_subscribers",
            SortModsBy.DateSubmitted => "modio_sort_type_date_submitted",
            _                        => "modio_sort_type_blank",
        };
    }
}
