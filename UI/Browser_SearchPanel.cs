using System.Collections.Generic;
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
        [Header("Search Panel")]
        [SerializeField] GameObject SearchPanel;
        [SerializeField] TMP_InputField SearchPanelField;
        [SerializeField] GameObject SearchPanelTagCategoryPrefab;
        [SerializeField] RectTransform SearchPanelTagViewport;
        [SerializeField] Transform SearchPanelTagParent;
        [SerializeField] GameObject SearchPanelTagPrefab;
        [SerializeField] Image SearchPanelLeftBumperIcon;
        [SerializeField] Image SearchPanelRightBumperIcon;

        internal static HashSet<Tag> searchFilterTags = new HashSet<Tag>();
        TagCategory[] tags;
        
#region Search Panel

        public void OpenSearchPanel()
        {
            //We are selecting before activating the object,
            //so that the input capture doesn't force the keyboard
            //to lock onto the object
            SearchPanel.SetActive(true);
            SelectionManager.Instance.SelectView(UiViews.SearchFilters);
            SearchPanelField.text = "";

            SearchPanelFieldNavigationLock();

            //ScrollRectViewHandler.Instance.CurrentViewportContent = SearchPanelTagParent;
            SetupSearchPanelTags();
        }

        void SearchPanelFieldNavigationLock()
        {
            Navigation nav = SearchPanelField.navigation;
            nav.mode = Navigation.Mode.None;
            SearchPanelField.navigation = nav;
        }

        void SearchPanelFieldNavigationUnlock()
        {
            Navigation nav = SearchPanelField.navigation;
            nav.mode = Navigation.Mode.Vertical;
            SearchPanelField.navigation = nav;
        }

        public void CloseSearchPanel()
        {
            InputReceiver.currentSelectedInputField = null;
            SearchPanel.SetActive(false);
            SelectionManager.Instance.SelectPreviousView();
        }

        public void ClearSearchFilter()
        {
            searchFilterTags = new HashSet<Tag>();
            SearchPanelField.SetTextWithoutNotify("");
            SetupSearchPanelTags();
        }

        void SetupSearchPanelTags()
        {
            if(tags != null)
            {
                CreateTagCategoryListItems(tags);
            }
            else
            {
                ModIOUnity.GetTagCategories(GetTags);
            }
        }

        void GetTags(ResultAnd<TagCategory[]> resultAndTags)
        {
            if(resultAndTags.result.Succeeded())
            {
                this.tags = resultAndTags.value;
                CreateTagCategoryListItems(resultAndTags.value);
            }
        }

        void CreateTagCategoryListItems(TagCategory[] tags)
        {
            if(tags == null || tags.Length < 1)
            {
                return;
            }

            ListItem.HideListItems<TagListItem>();
            ListItem.HideListItems<TagCategoryListItem>();
            TagJumpToSelection.ClearCache();

            List<Selectable> listItems = new List<Selectable>();

            //this can add the items to a list
            foreach(TagCategory category in tags)
            {
                ListItem categoryListItem = ListItem.GetListItem<TagCategoryListItem>(SearchPanelTagCategoryPrefab, SearchPanelTagParent, colorScheme);
                categoryListItem.Setup(category.name);

                IEnumerable<ListItem> v = CreateTagListItems(category);
                listItems.AddRange(v.Select(x => x.selectable));
            }
            UpdateSearchPanelBumperIcons();
            ReorderAndSetNavigation(listItems);
            LayoutRebuilder.ForceRebuildLayoutImmediate(SearchPanelTagParent as RectTransform);

            SearchPanelFieldNavigationUnlock();
        }

        void ReorderAndSetNavigation(IEnumerable<Selectable> items)
        {
            var orderedItems = items.OrderBy(x => x.transform.GetSiblingIndex()).ToList();

            //Clear any previous navigation properties
            orderedItems.ForEach(x =>
            {
                var nav = x.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = null;
                nav.selectOnDown = null;
                nav.selectOnRight = null;
                nav.selectOnLeft = null;
                x.navigation = nav;
            });

            //Link up next/prev navigation links (if possible)
            for(int i = 0; i < orderedItems.Count(); i++)
            {
                var currentNav = orderedItems[i].navigation;

                if(GetWithinBoundsOfList(orderedItems, i - 1, out var previous))
                {
                    currentNav.selectOnUp = previous;

                    var previousNav = previous.navigation;
                    previousNav.selectOnDown = orderedItems[i];
                    previous.navigation = previousNav;                    
                }
                else
                {
                    //Upmost nagivation leads to the search panel field
                    currentNav.selectOnUp = SearchPanelField;
                }

                if(GetWithinBoundsOfList(orderedItems, i + 1, out var next))
                {
                    currentNav.selectOnDown = next;

                    var nextNav = next.navigation;
                    nextNav.selectOnDown = orderedItems[i];
                    next.navigation = nextNav;
                }
                else
                {
                    //Null down navigation for last field, we access the functionality
                    //through controller buttons
                    currentNav.selectOnDown = null;
                }
                    

                orderedItems[i].navigation = currentNav;
            }
        }

        /// <summary>
        /// Attempt to get an indexed T
        /// Example:
        /// if(GetWithinBoundsOfList(items, i + 1, out var next)) { }
        /// </summary>
        /// <returns>true the item exists</returns>
        bool GetWithinBoundsOfList<T>(List<T> list, int index, out T item)
        {
            item = default(T);
            if(index >= 0 && index < list.Count())
            {
                item = list[index];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates and sets up data for list items
        /// </summary>
        /// <returns>Fetched items</returns>
        IEnumerable<ListItem> CreateTagListItems(TagCategory category)
        {            
            bool setJumpTo = false;
            
            foreach(ModIO.Tag tag in category.tags)
            {
                ListItem tagListItem = ListItem.GetListItem<TagListItem>(SearchPanelTagPrefab, SearchPanelTagParent, colorScheme);
                tagListItem.Setup(tag.name, category.name);
                tagListItem.SetViewportRestraint(SearchPanelTagParent as RectTransform, SearchPanelTagViewport);
                
                if(!setJumpTo)
                {
                    tagListItem.Setup();
                    setJumpTo = true;
                }

                yield return tagListItem;
            }
        }

        public void ApplySearchFilter()
        {
            OpenSearchResults(SearchPanelField.text);
        }

#endregion

    }
}
