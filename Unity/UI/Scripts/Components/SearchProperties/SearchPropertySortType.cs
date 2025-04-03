using System;
using Modio.Mods;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertySortType : ISearchProperty
    {
        [SerializeField] TMP_Text _searchText;

        public void OnSearchUpdate(ModioUISearch search)
        {
            //TODO: Localization support here
            if (search.SortByOverriden)
            {
                SortModsBy sortModsBy = search.LastSearchFilter.SortBy;
                var sortByText = sortModsBy == SortModsBy.DateSubmitted ? "Date Submitted" : sortModsBy.ToString();
                _searchText.text = $"<b>SORT:</b> {sortByText}";
            }
            else
            {
                _searchText.text = "SORT";
            }
        }
    }
}
