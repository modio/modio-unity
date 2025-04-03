using System;
using Modio.Unity.UI.Input;
using Modio.Unity.UI.Panels;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyPagedSearch : ISearchProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] TMP_Text _pageCountText;
        [SerializeField] string _pageCountString = "Page {0} of {1}";

        [SerializeField] Button _prevPage;
        [SerializeField] Button _nextPage;

        [SerializeField] ModioPanelBase _whenPanelFocused;

        ModioUISearch _search;

        public void OnSearchUpdate(ModioUISearch search)
        {
            _search = search;

            if (_pageCountText != null)
            {
                var pageIndex = search.LastSearchFilter.PageIndex + 1;
                var pageCount = search.LastSearchResultPageCount;
                _pageCountText.text = string.Format(_pageCountString, pageIndex, pageCount);
            }
        }

        public void Start()
        {
            if (_prevPage != null) _prevPage.onClick.AddListener(OnPrevPageClicked);
            if (_nextPage != null) _nextPage.onClick.AddListener(OnNextPageClicked);
        }

        void OnPrevPageClicked()
        {
            if (_whenPanelFocused != null && !_whenPanelFocused.HasFocus) return;

            var currentPageIndex = _search.LastSearchFilter.PageIndex;
            if (currentPageIndex > 0) _search.SetPageForCurrentSearch(currentPageIndex - 1);
        }

        void OnNextPageClicked()
        {
            if (_whenPanelFocused != null && !_whenPanelFocused.HasFocus) return;

            var currentPageIndex = _search.LastSearchFilter.PageIndex;

            if (currentPageIndex + 1 < _search.LastSearchResultPageCount)
                _search.SetPageForCurrentSearch(currentPageIndex + 1);
        }

        public void OnDestroy() { }

        public void OnEnable()
        {
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.SearchPageLeft,  OnPrevPageClicked);
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.SearchPageRight, OnNextPageClicked);
        }

        public void OnDisable()
        {
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.SearchPageLeft,  OnPrevPageClicked);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.SearchPageRight, OnNextPageClicked);
        }
    }
}
