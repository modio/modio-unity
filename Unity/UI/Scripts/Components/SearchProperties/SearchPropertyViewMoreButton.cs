using System;
using Modio.Mods;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyViewMoreButton : ISearchProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] GameObject[] _displayWhenMoreResults;

        [SerializeField] Button _viewAllResultsButton;
        ModioUISearch _search;

        public void OnSearchUpdate(ModioUISearch search)
        {
            _search = search;

            foreach (GameObject go in _displayWhenMoreResults)
                go.SetActive(!search.ModioSearch.IsSearching && search.ModioSearch.CanGetMoreResults);
        }

        public void Start()
        {
        }

        public void OnDestroy()
        {
        }

        public void OnEnable()
        {
            if (_viewAllResultsButton != null) _viewAllResultsButton.onClick.AddListener(ViewAllResultsClicked);
        }

        public void OnDisable()
        {
            if (_viewAllResultsButton != null) _viewAllResultsButton.onClick.RemoveListener(ViewAllResultsClicked);
        }

        void ViewAllResultsClicked()
        {
            var searchWith = ModioUISearch.Default;
            ModSearchFilter filter = _search.LastSearchSettingsFrom.GetSearchFilter(searchWith.DefaultPageSize);
            searchWith.SetSearch(filter, _search.ModioSearch.LastSearchPreset, settingsFrom: _search.LastSearchSettingsFrom);
        }
    }
}
