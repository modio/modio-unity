using System.Collections;
using System.Linq;
using ModIO;
using ModIO.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    public class SearchResults : SelfInstancingMonoSingleton<SearchResults>
    {
        [Header("Search Results Panel")]
        [SerializeField] public GameObject SearchResultsPanel;
        [SerializeField] Image SearchResultsHeaderBackground;
        [SerializeField] GameObject SearchResultsHeaderRefineButton;
        [SerializeField] RectTransform SearchResultsContentRefineButton;
        [SerializeField] GameObject SearchResultsListItemPrefab;
        [SerializeField]
        public Transform SearchResultsListItemParent;
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

        internal Translation SearchResultsFoundTextTranslation = null;
        internal Translation SearchResultsNumberOfOtherTagsTranslation = null;
        internal Translation SearchResultsEndOfResultsHeaderTranslation = null;
        internal Translation SearchResultsEndOfResultsTextTranslation = null;

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
        internal void Open(string searchPhrase)
        {
            ClearButtonNavigation();

            lastUsedSearchPhrase = searchPhrase;
            Navigating.GoToPanel(SearchResultsPanel);

            Refresh();
            SelectionManager.Instance.SelectView(UiViews.SearchResults);
        }

        internal void OpenWithoutRefreshing()
        {
            SelectionManager.Instance.SelectView(UiViews.SearchResults);
            Navigating.GoToPanel(SearchResultsPanel);
        }

        internal SearchFilter GetFilter(int page = 0, string searchPhrase = null)
        {
            searchPhrase = searchPhrase ?? lastUsedSearchPhrase;

            SearchFilter filter = new SearchFilter();

            switch(SearchResultsSortByDropdown.value)
            {
                case 0:
                    filter.SetSortBy(SortModsBy.Popular);
                    break;
                case 1:
                    filter.SetSortBy(SortModsBy.Downloads);
                    break;
                case 2:
                    filter.SetSortBy(SortModsBy.Subscribers);
                    break;
                case 3:
                    filter.SetSortBy(SortModsBy.Rating);
                    break;
            }

            filter.AddSearchPhrase(searchPhrase);
            foreach(var tag in SearchPanel.searchFilterTags)
            {
                filter.AddTag(tag.name);
            }

            filter.SetPageIndex(page);
            filter.SetPageSize(100);

            return filter;
        }

        public void Refresh()
        {
            // Reset results while waiting
            SearchResultsMainTag.SetActive(false);
            SearchResultsSearchPhrase.SetActive(false);
            Translation.Get(SearchResultsFoundTextTranslation, "", SearchResultsFoundText);
            Translation.Get(SearchResultsEndOfResultsHeaderTranslation, "", SearchResultsEndOfResultsHeader);
            Translation.Get(SearchResultsEndOfResultsTextTranslation, "", SearchResultsEndOfResultsText);

            searchResultsStatus = SearchResultsStatus.GettingFirstResults;
            ListItem.HideListItems<SearchResultListItem>();
            SearchResultsNoResultsText.SetActive(false);
            ProcessingAnimation.gameObject.SetActive(true);
            ModIOUnity.GetMods(GetFilter(), Get);
        }

        public void OnScrollValueChange()
        {
            CheckToLoadMoreModsOrForFooterDisplay();
            OnScrollValueChangeHeaderCheck();
            CheckRefineButtonDisplay();
        }

        /// <summary>
        /// Based on the scroll position this will either load an additional 100 mods to the results
        /// list or display the appropriate footer message for number of results
        /// </summary>
        void CheckToLoadMoreModsOrForFooterDisplay()
        {
            if(!moreResultsToShow || searchResultsStatus != SearchResultsStatus.RetrievedFirstResults)
            {
                return;
            }

            RectTransformOverlap container = new RectTransformOverlap(SearchResultsListItemParent as RectTransform);
            RectTransformOverlap screen = new RectTransformOverlap(Home.Instance.BrowserPanel.transform as RectTransform);

            // Check how close we are to the bottom. If we are half a screen height from the last
            // result to display, lets begin loading in more results
            if(container.yMin < 0 && Mathf.Abs(container.yMin) < screen.height / 2f)
            {
                searchResultsStatus = SearchResultsStatus.GettingMoreResults;
                Home.Instance.AddPlaceholdersToList<SearchResultListItem>(SearchResultsListItemParent, SearchResultsListItemPrefab, (int)numberOfRemainingResultsToShow);
                ModIOUnity.GetMods(GetFilter(1), Get);
            }
        }

        /// <summary>
        /// Checks the scroll position and determines whether or not to begin an alpha transition of
        /// the header background image (opaque when scrolled, transparent when at viewport top)
        /// </summary>
        void OnScrollValueChangeHeaderCheck()
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
                searchResultsHeaderTransition = ImageTransitions.Alpha(SearchResultsHeaderBackground, targetAlpha);
                StartCoroutine(searchResultsHeaderTransition);
            }
        }

        /// <summary>
        /// Checks whether or not to use the header refine button or the content refine button based
        /// off of the scroll position
        /// </summary>
        void CheckRefineButtonDisplay()
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

        void Get(ResultAnd<ModPage> response)
        {
            if(response.result.Succeeded())
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

                string numberOfResults = Utility.GenerateHumanReadableNumber(response.value.totalSearchResultsFound);
                numberOfRemainingResultsToShow = response.value.totalSearchResultsFound - 100;
                moreResultsToShow = numberOfRemainingResultsToShow > 0;

                // show searched tag info
                var enumerator = SearchPanel.searchFilterTags.GetEnumerator();
                if(enumerator.MoveNext())
                {
                    // set active tag info
                    SearchResultsMainTagName.text = enumerator.Current.name;
                    SearchResultsMainTagCategoryName.text = enumerator.Current.category;

                    SearchResultsMainTag.SetActive(true);

                    Translation.Get(SearchResultsNumberOfOtherTagsTranslation, "and {number} other tags", SearchResultsNumberOfOtherTags, $"{SearchPanel.searchFilterTags.Count - 1}");

                    LayoutRebuilder.ForceRebuildLayoutImmediate(SearchResultsMainTag.transform.parent as RectTransform);
                }
                else
                {
                    SearchResultsMainTag.SetActive(false);
                }

                // display the used search phrase
                if(SearchPanel.Instance.SearchPanelField.text.Length > 0)
                {
                    SearchResultsSearchPhrase.SetActive(true);
                    SearchResultsSearchPhraseText.text = SearchPanel.Instance.SearchPanelField.text;
                }
                else
                {
                    SearchResultsSearchPhrase.SetActive(false);
                }

                // Setup the end of results footer and text
                if(response.value.totalSearchResultsFound == 0)
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
                    long numberOfDisplayedMods = response.value.totalSearchResultsFound > 200 ? 200 : response.value.totalSearchResultsFound;
                    Translation.Get(SearchResultsEndOfResultsHeaderTranslation, "You've gone through {number} mods", SearchResultsEndOfResultsHeader, $"{numberOfDisplayedMods}");
                    Translation.Get(SearchResultsEndOfResultsTextTranslation, "Let's refine your search if you haven't found what you were looking for.", SearchResultsEndOfResultsText);
                    SearchResultsNoResultsText.SetActive(false);
                    SearchResultsEndOfResults.SetActive(true);
                }

                //Translation.Get(SearchResultsFoundTextTranslation, "", SearchResultsFoundText);
                if(string.IsNullOrWhiteSpace(lastUsedSearchPhrase))
                {
                    Translation.Get(SearchResultsFoundTextTranslation, "{num} Mods found", SearchResultsFoundText, $"{numberOfResults}");
                }
                else
                {
                    Translation.Get(SearchResultsFoundTextTranslation, "{num} Mods found for {lastUsedSearchPhrase}", SearchResultsFoundText, $"{numberOfResults}", $"\"{lastUsedSearchPhrase}\"");
                }

                // Create list items for the mod profiles we now have
                Populate(response.value.modProfiles);
            }
            else
            {
                Translation.Get(SearchResultsFoundTextTranslation, "A problem occurred", SearchResultsFoundText);
            }

            ProcessingAnimation.gameObject.SetActive(false);
        }

        void Populate(ModProfile[] mods)
        {
            for(int i = 0; i < mods.Length; i++)
            {
                ListItem li = ListItem.GetListItem<SearchResultListItem>(SearchResultsListItemPrefab, SearchResultsListItemParent, SharedUi.colorScheme);
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

            UpdateButtonNavigation();
        }

        #endregion

        #region Button navigation helper methods

        void ClearButtonNavigation()
        {
            var refineNav = SearchResultsRefineFilter.navigation;
            refineNav.selectOnDown = null;
            SearchResultsRefineFilter.navigation = refineNav;

            var filterNav = SearchResultsFilterBy.navigation;
            filterNav.selectOnDown = null;
            SearchResultsFilterBy.navigation = filterNav;
        }

        void UpdateButtonNavigation()
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
