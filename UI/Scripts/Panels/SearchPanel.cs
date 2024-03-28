using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModIO;
using ModIO.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    class SearchPanel : SelfInstancingMonoSingleton<SearchPanel>
    {
        [Header("Search Panel")]
        [SerializeField] public GameObject SearchPanelGameObject;
        [SerializeField] public TMP_InputField SearchPanelField;
        [SerializeField] GameObject SearchPanelTagCategoryPrefab;
        [SerializeField] RectTransform SearchPanelTagViewport;
        [SerializeField] Transform SearchPanelTagParent;
        [SerializeField] GameObject SearchPanelTagPrefab;
        [SerializeField] public Image SearchPanelLeftBumperIcon;
        [SerializeField] public Image SearchPanelRightBumperIcon;

        public static HashSet<Tag> searchFilterTags = new HashSet<Tag>();
        public static bool searchFilterFree = true;
        public static bool searchFilterPremium = true;
        internal TagCategory[] tags;
        bool gettingTags;

        public void Open()
        {
            //We are selecting before activating the object,
            //so that the input capture doesn't force the keyboard
            //to lock onto the object
            SearchPanelGameObject.SetActive(true);
            SelectionManager.Instance.SelectView(UiViews.SearchFilters);
            SearchPanelField.text = "";

            FieldNavigationLock();

            //ScrollRectViewHandler.Instance.CurrentViewportContent = SearchPanelTagParent;
            SetupTags();
        }

        void FieldNavigationLock()
        {
            Navigation nav = SearchPanelField.navigation;
            nav.mode = Navigation.Mode.None;
            SearchPanelField.navigation = nav;
        }

        void FieldNavigationUnlock(List<Selectable> listItems)
        {
            Navigation nav = SearchPanelField.navigation;
            nav.mode = Navigation.Mode.Explicit;
            if(listItems.Count > 0)
            {
                nav.selectOnDown = listItems[0];
            }
            nav.selectOnUp = null;
            nav.selectOnRight = null;
            nav.selectOnLeft = null;
            SearchPanelField.navigation = nav;
        }

        public void Close()
        {
            InputReceiver.currentSelectedInputField = null;
            SearchPanelGameObject.SetActive(false);
            SelectionManager.Instance.SelectPreviousView();
        }

        public void ClearFilter()
        {
            searchFilterTags = new HashSet<Tag>();
            SearchPanelField.SetTextWithoutNotify("");
            SetupTags();
        }

        public void SetupTags()
        {
            if(tags != null)
            {
                CreateTagCategoryListItems(tags);
            }
            else
            {
                UpdateTags();
            }
        }

        internal async Task WaitForTagsToUpdate()
        {
            if(!gettingTags && tags == null)
            {
                UpdateTags();
            }
            while(gettingTags)
            {
                await Task.Yield();
            }
        }

        void UpdateTags()
        {
            gettingTags = true;
            ModIOUnity.GetTagCategories(ReceiveTags);
        }

        void ReceiveTags(ResultAnd<TagCategory[]> resultAndTags)
        {
            if(resultAndTags.result.Succeeded())
            {
                tags = resultAndTags.value;
                CreateTagCategoryListItems(resultAndTags.value);
            }
            gettingTags = false;
        }

        internal List<string> GetHiddenTags()
        {
            List<string> hidden = new List<string>();
            foreach(var category in tags)
            {
                if(category.hidden)
                {
                    foreach(var tag in category.tags)
                    {
                        hidden.Add(tag.name);
                    }
                }
            }
            return hidden;
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
                if(category.hidden)
                {
                    continue;
                }

                ListItem categoryListItem = ListItem.GetListItem<TagCategoryListItem>(SearchPanelTagCategoryPrefab, SearchPanelTagParent, SharedUi.colorScheme);
                categoryListItem.Setup(category.name);

                IEnumerable<ListItem> v = CreateTagListItems(category);
                listItems.AddRange(v.Select(x => x.selectable));
            }
            UpdateBumperIcons();

            var orderedItems = listItems.OrderBy(x => x.transform.GetSiblingIndex()).ToList();
            ReorderAndSetNavigation(orderedItems);
            LayoutRebuilder.ForceRebuildLayoutImmediate(SearchPanelTagParent as RectTransform);
            FieldNavigationUnlock(orderedItems);
        }

        void ReorderAndSetNavigation(List<Selectable> items)
        {
            //Clear any previous navigation properties
            items.ForEach(x =>
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
            for(int i = 0; i < items.Count(); i++)
            {
                var currentNav = items[i].navigation;

                if(GetWithinBoundsOfList(items, i - 1, out var previous))
                {
                    currentNav.selectOnUp = previous;

                    var previousNav = previous.navigation;
                    previousNav.selectOnDown = items[i];
                    previous.navigation = previousNav;
                }
                else
                {
                    //Upmost nagivation leads to the search panel field
                    currentNav.selectOnUp = SearchPanelField;
                }

                if(GetWithinBoundsOfList(items, i + 1, out var next))
                {
                    currentNav.selectOnDown = next;

                    var nextNav = next.navigation;
                    nextNav.selectOnDown = items[i];
                    next.navigation = nextNav;
                }
                else
                {
                    //Null down navigation for last field, we access the functionality
                    //through controller buttons
                    currentNav.selectOnDown = null;
                }

                items[i].navigation = currentNav;
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
                ListItem tagListItem = ListItem.GetListItem<TagListItem>(SearchPanelTagPrefab, SearchPanelTagParent, SharedUi.colorScheme);
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

        public void ApplyFilter()
        {
            SearchResults.Instance.Open(SearchPanelField.text);
        }

        /// <summary>
        /// This updates the display of the bumper icons in the search panel, showing whether or not
        /// you can continue jumping to the next tag category with the specified bumper input.
        /// </summary>
        internal void UpdateBumperIcons()
        {
            Color left = SearchPanelLeftBumperIcon.color;
            left.a = TagJumpToSelection.CanTabLeft() ? 1f : 0.2f;
            SearchPanelLeftBumperIcon.color = left;

            Color right = SearchPanelRightBumperIcon.color;
            right.a = TagJumpToSelection.CanTabRight() ? 1f : 0.2f;
            SearchPanelRightBumperIcon.color = right;
        }


        internal void ToggleState()
        {
            if(SearchPanelGameObject.activeSelf)
            {
                Instance.Close();
            }
            else
            {
                Instance.Open();
            }
        }
    }
}
