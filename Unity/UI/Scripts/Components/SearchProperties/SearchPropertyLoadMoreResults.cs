using System;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyLoadMoreResults : ISearchProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] GameObject[] _displayWhenMoreResults;

        [SerializeField] Button _loadMoreResultsButton;
        ModioUISearch _search;

        public void OnSearchUpdate(ModioUISearch search)
        {
            _search = search;

            foreach (var go in _displayWhenMoreResults)
            {
                go.SetActive(!search.IsSearching && search.CanGetMoreMods);
            }
        }

        public void Start() { }

        public void OnDestroy() { }

        public void OnEnable()
        {
            if (_loadMoreResultsButton != null) _loadMoreResultsButton.onClick.AddListener(LoadMoreClicked);
        }

        public void OnDisable()
        {
            if (_loadMoreResultsButton != null) _loadMoreResultsButton.onClick.RemoveListener(LoadMoreClicked);
        }

        void LoadMoreClicked()
        {
            _search.GetNextPageAdditivelyForLastSearch();
        }
    }
}
