using System;
using Modio.Mods;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.Serialization;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertySortByToggle : ISearchProperty
    {
        [SerializeField] SortModsBy _sortBy;

        [SerializeField] ModioUIToggle _toggle;
        
        [SerializeField] GameObject _enableWhenAscending;
        [SerializeField] GameObject _enableWhenDescending;
        
        ModioUISearch _search;

        public void OnSearchUpdate(ModioUISearch search)
        {
            this._search = search;
            bool isCurrentSort = this._search.ModioSearch.LastSearchFilter.SortBy == _sortBy;
            bool isSortAscending = this._search.ModioSearch.LastSearchFilter.IsSortAscending;

            if (_enableWhenAscending != null)
                _enableWhenAscending.SetActive(isCurrentSort && isSortAscending);
            if (_enableWhenDescending != null)
                _enableWhenDescending.SetActive(isCurrentSort && !isSortAscending);

            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            _toggle.isOn = isCurrentSort;
            _toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        void OnToggleChanged(bool isOn)
        {
            bool isCurrentSort = _search.ModioSearch.LastSearchFilter.SortBy == _sortBy;
            bool isSortAscending = _search.ModioSearch.LastSearchFilter.IsSortAscending;

            const bool defaultToAscending = false;

            if(!isCurrentSort)
            {
                _search.ModioSearch.ApplySortBy(_sortBy, defaultToAscending);
            }
            else if (isSortAscending == defaultToAscending)
            {
                _search.ModioSearch.ApplySortBy(_sortBy, !defaultToAscending);
            }
            else
            {
                _search.ModioSearch.ClearCustomOrdering();
            }
        }
    }
}
