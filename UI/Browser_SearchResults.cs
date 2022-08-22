using System;
using System.Collections;
using System.Linq;
using ModIO;
using ModIOBrowser.Implementation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    /// <summary>
    ///the main interface for interacting with the Mod Browser UI
    /// </summary>
    public partial class Browser
    {
        [Header("Search Results Panel")]
        [SerializeField] GameObject SearchResultsPanel;
        [SerializeField] Image SearchResultsHeaderBackground;
        [SerializeField] GameObject SearchResultsHeaderRefineButton;
        [SerializeField] RectTransform SearchResultsContentRefineButton;
        [SerializeField] GameObject SearchResultsListItemPrefab;
        [SerializeField] Transform SearchResultsListItemParent;
        [SerializeField] Scrollbar SearchResultsScrollBar;
        [SerializeField] TMP_Text SearchResultsFoundText;
        [SerializeField] TMP_Text SearchResultsMainTagName;
        [SerializeField] TMP_Text SearchResultsMainTagCategoryName;
        [SerializeField] GameObject SearchResultsMainTag;
        [SerializeField] TMP_Text SearchResultsNumberOfOtherTags;
        [SerializeField] GameObject SearchResultsSearchPhrase;
        [SerializeField] TMP_Text SearchResultsSearchPhraseText;
        [SerializeField] TMP_Dropdown SearchResultsSortByDropdown;
        [SerializeField] GameObject SearchResultsEndOfResults;
        [SerializeField] GameObject SearchResultsNoResultsText;
        [SerializeField] TMP_Text SearchResultsEndOfResultsHeader;
        [SerializeField] TMP_Text SearchResultsEndOfResultsText;
        [SerializeField] Selectable SearchResultsRefineFilter;
        [SerializeField] Selectable SearchResultsFilterBy;
        [SerializeField] GameObject ProcessingAnimation;

        bool moreResultsToShow;
        long numberOfRemainingResultsToShow;
        string lastUsedSearchPhrase;
        SearchResultsStatus searchResultsStatus;
        IEnumerator searchResultsHeaderTransition;
        float searchResultsLastAlphaTarget = -1;
        
        enum SearchResultsStatus
        {
            GettingFirstResults,
            RetrievedFirstResults,
            GettingMoreResults,
            RetrievedAllResults
        }

#region Search Results

        /// <summary>
        /// This opens the search results panel as well as inputs a search phrase to be used
        /// </summary>
        /// <param name="searchPhrase"></param>
        internal void OpenSearchResults(string searchPhrase)
        {
            SearchResults_ClearButtonNavigation();

            lastUsedSearchPhrase = searchPhrase;
            GoToPanel_deprecating(SearchResultsPanel);

            RefreshSearch();
            SelectionManager.Instance.SelectView(UiViews.SearchResults);
        }

        internal void OpenSearchResultsWithoutRefreshing()
        {            
            SelectionManager.Instance.SelectView(UiViews.SearchResults);
            GoToPanel_deprecating(SearchResultsPanel);
        }

        internal SearchFilter GetSearchResultFilter(int page = 0, string searchPhrase = null)
        {
            searchPhrase = searchPhrase ?? lastUsedSearchPhrase;

            SearchFilter filter = new SearchFilter();

            switch(SearchResultsSortByDropdown.value)
            {
                case 0:
                    filter.SortBy(SortModsBy.Popular);
                    break;
                case 1:
                    filter.SortBy(SortModsBy.Downloads);
                    break;
                case 2:
                    filter.SortBy(SortModsBy.Subscribers);
                    break;
                case 3:
                    filter.SortBy(SortModsBy.Rating);
                    break;
            }

            filter.AddSearchPhrase(searchPhrase);
            foreach(var tag in searchFilterTags)
            {
                filter.AddTag(tag.name);
            }

            filter.SetPageIndex(page);
            filter.SetPageSize(100);

            return filter;
        }

        public void RefreshSearch()
        {
            // Reset results while waiting
            SearchResultsMainTag.SetActive(false);
            SearchResultsSearchPhrase.SetActive(false);
            SearchResultsFoundText.text = "";
            SearchResultsEndOfResultsHeader.text = "";
            SearchResultsEndOfResultsText.text = "";
            
            searchResultsStatus = SearchResultsStatus.GettingFirstResults;
            ListItem.HideListItems<SearchResultListItem>();
            SearchResultsNoResultsText.SetActive(false);
            ProcessingAnimation.gameObject.SetActive(true);
            ModIOUnity.GetMods(GetSearchResultFilter(), GetSearchResult);
        }

        public void SearchResultsOnScroll()
        {
            SearchResultsCheckToLoadMoreModsOrForFooterDisplay();
            SearchResultsOnScrollValueChangeHeaderCheck();
            SearchResultsCheckRefineButtonDisplay();
        }

        /// <summary>
        /// Based on the scroll position this will either load an additional 100 mods to the results
        /// list or display the appropriate footer message for number of results
        /// </summary>
        void SearchResultsCheckToLoadMoreModsOrForFooterDisplay()
        {
            if(!moreResultsToShow || searchResultsStatus != SearchResultsStatus.RetrievedFirstResults)
            {
                return;
            }
            
            RectTransformOverlap container = new RectTransformOverlap(SearchResultsListItemParent as RectTransform);
            RectTransformOverlap screen = new RectTransformOverlap(BrowserPanel.transform as RectTransform);

            // Check how close we are to the bottom. If we are half a screen height from the last
            // result to display, lets begin loading in more results
            if(container.yMin < 0 && Mathf.Abs(container.yMin) < screen.height / 2f)
            {
                searchResultsStatus = SearchResultsStatus.GettingMoreResults;
                AddPlaceholdersToList<SearchResultListItem>(SearchResultsListItemParent, SearchResultsListItemPrefab, (int)numberOfRemainingResultsToShow);
                ModIOUnity.GetMods(GetSearchResultFilter(1), GetSearchResult);
            }
        }
        
        /// <summary>
        /// Checks the scroll position and determines whether or not to begin an alpha transition of
        /// the header background image (opaque when scrolled, transparent when at viewport top)
        /// </summary>
        void SearchResultsOnScrollValueChangeHeaderCheck()
        {
            float targetAlpha = -1f;
            
            // Get the target alpha based on what the scrollbar value is
            if(SearchResultsScrollBar.value < 1f)
            {
                targetAlpha = SearchResultsHeaderBackground.color.a == 1f ? targetAlpha : 1f;
            }
            else
            {
                targetAlpha = SearchResultsHeaderBackground.color.a == 0f ? targetAlpha : 0f;
            }

            // If the target alpha needs to change, start the transition coroutine here
            if(targetAlpha != -1f && targetAlpha != searchResultsLastAlphaTarget)
            {
                searchResultsLastAlphaTarget = targetAlpha;
                if(searchResultsHeaderTransition != null)
                {
                    StopCoroutine(searchResultsHeaderTransition);
                }
                searchResultsHeaderTransition = TransitionImageAlpha(SearchResultsHeaderBackground, targetAlpha);
                StartCoroutine(searchResultsHeaderTransition);
            }
        }

        /// <summary>
        /// Checks whether or not to use the header refine button or the content refine button based
        /// off of the scroll position
        /// </summary>
        void SearchResultsCheckRefineButtonDisplay()
        {
            float refineButtonY = SearchResultsContentRefineButton.position.y 
                                  - (SearchResultsContentRefineButton.sizeDelta.y 
                                     * SearchResultsContentRefineButton.pivot.y);

            if(refineButtonY > Screen.height - SearchResultsHeaderBackground.rectTransform.sizeDelta.y)
            {
                // NOTE: checking activeSelf may seem unnecessary but reduces extra UI Dirty redraws
                if(SearchResultsContentRefineButton.gameObject.activeSelf)
                {
                    SearchResultsContentRefineButton.gameObject.SetActive(false);
                }
                if(!SearchResultsHeaderRefineButton.activeSelf)
                {
                    SearchResultsHeaderRefineButton.SetActive(true);
                }
            }
            else
            {
                // NOTE: checking activeSelf may seem unnecessary but reduces extra UI Dirty redraws
                if(!SearchResultsContentRefineButton.gameObject.activeSelf)
                {
                    SearchResultsContentRefineButton.gameObject.SetActive(true);
                }
                if(SearchResultsHeaderRefineButton.activeSelf)
                {
                    SearchResultsHeaderRefineButton.SetActive(false);
                }
            }
        }

        internal void GetSearchResult(Result result, ModPage modPage)
        {
            if(result.Succeeded())
            {
                // set the status so we know how to control 
                if(searchResultsStatus == SearchResultsStatus.GettingFirstResults)
                {
                    searchResultsStatus = SearchResultsStatus.RetrievedFirstResults;
                }
                else if(searchResultsStatus == SearchResultsStatus.GettingMoreResults)
                {
                    searchResultsStatus = SearchResultsStatus.RetrievedAllResults;
                }

                string numberOfResults = Utility.GenerateHumanReadableNumber(modPage.totalSearchResultsFound);
                numberOfRemainingResultsToShow = modPage.totalSearchResultsFound - 100;
                moreResultsToShow = numberOfRemainingResultsToShow > 0;

                // show searched tag info
                var enumerator = searchFilterTags.GetEnumerator();
                if(enumerator.MoveNext())
                {
                    // set active tag info
                    SearchResultsMainTagName.text = enumerator.Current.name;
                    SearchResultsMainTagCategoryName.text = enumerator.Current.category;
                    
                    SearchResultsMainTag.SetActive(true);

                    SearchResultsNumberOfOtherTags.text = $"and {searchFilterTags.Count - 1} other tags";
                    
                    LayoutRebuilder.ForceRebuildLayoutImmediate(SearchResultsMainTag.transform.parent as RectTransform);
                }
                else
                {
                    SearchResultsMainTag.SetActive(false);
                }

                // display the used search phrase
                if(SearchPanelField.text.Length > 0)
                {
                    SearchResultsSearchPhrase.SetActive(true);
                    SearchResultsSearchPhraseText.text = SearchPanelField.text;
                }
                else
                {
                    SearchResultsSearchPhrase.SetActive(false);
                }

                // Setup the end of results footer and text
                if(modPage.totalSearchResultsFound == 0)
                {
                    SearchResultsNoResultsText.SetActive(true);
                    SearchResultsEndOfResults.SetActive(false);
                }
                else if(moreResultsToShow && searchResultsStatus == SearchResultsStatus.RetrievedFirstResults)
                {
                    SearchResultsEndOfResults.gameObject.SetActive(false);
                }
                else
                {
                    long numberOfDisplayedMods = modPage.totalSearchResultsFound > 200 ? 200 : modPage.totalSearchResultsFound;
                    SearchResultsEndOfResultsHeader.text = $"You've gone through {numberOfDisplayedMods} mods";
                    SearchResultsEndOfResultsText.text = "Let's refine your search if you haven't"
                                                         + " found what you were looking for.";
                    SearchResultsNoResultsText.SetActive(false);
                    SearchResultsEndOfResults.SetActive(true);
                }

                SearchResultsFoundText.text = string.IsNullOrWhiteSpace(lastUsedSearchPhrase)
                    ? $"{numberOfResults} Mods found"
                    : $"{numberOfResults} Mods found for \"{lastUsedSearchPhrase}\"";

                // Create list items for the mod profiles we now have
                PopulateSearchResults(modPage.modProfiles);                
            }
            else
            {
                SearchResultsFoundText.text = $"A problem occurred";
            }

            ProcessingAnimation.gameObject.SetActive(false);
        }

        internal void PopulateSearchResults(ModProfile[] mods)
        {
            for(int i = 0; i < mods.Length; i++)
            {
                ListItem li = ListItem.GetListItem<SearchResultListItem>(SearchResultsListItemPrefab, SearchResultsListItemParent, colorScheme);
                li.Setup(mods[i]);
                li.SetViewportRestraint(SearchResultsListItemParent as RectTransform, null);

                // if you're the last row, have a larger viewport offset for bottom to show 'end of results'
                int lastRowSize = mods.Length % 5;
                lastRowSize = lastRowSize == 0 ? 5 : lastRowSize;

                if(i >= mods.Length - lastRowSize)
                {
                    // TODO i'm not a fan of using a GC method here, but it's max 5x for the entire
                    // search. replace this in future
                    li.gameObject.GetComponent<SearchResultListItem>().SetAsLastRowItem();
                }
            }

            // TODO show tags that are part of the search

            ListItem.HideListItems<SearchResultListItem>(true);

            SearchResults_UpdateButtonNavigation();
        }

        #endregion

#region Button navigation helper methods

        void SearchResults_ClearButtonNavigation()
        {
            var refineNav = SearchResultsRefineFilter.navigation;
            refineNav.selectOnDown = null;
            SearchResultsRefineFilter.navigation = refineNav;

            var filterNav = SearchResultsFilterBy.navigation;
            filterNav.selectOnDown = null;
            SearchResultsFilterBy.navigation = filterNav;
        }

        void SearchResults_UpdateButtonNavigation()
        {
            var childrenBegin = 2;
            var childrenEnd = 7;

            var items = ListItem.Where<SearchResultListItem>(x =>
                {
                    var query = x.transform.GetSiblingIndex() > childrenBegin
                             && x.transform.GetSiblingIndex() <= childrenEnd;

                    return query;
                })
                .OrderByDescending(x => x.transform.GetSiblingIndex())
                .ToList();

            if(items.Count > 0)
            {
                var refineNav = SearchResultsRefineFilter.navigation;
                refineNav.selectOnDown = items[0].selectable;
                SearchResultsRefineFilter.navigation = refineNav;

                var filterNav = SearchResultsFilterBy.navigation;
                filterNav.selectOnDown = items[0].selectable;
                SearchResultsFilterBy.navigation = filterNav;
            }            
        }

#endregion
    }
}
