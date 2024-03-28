using System;
using System.Collections;
using System.Collections.Generic;
using ModIO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    public class ModListRow : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] GameObject ErrorPanel;
        [SerializeField] GameObject LoadingPanel;
        [SerializeField] GameObject RowPanel;
        [SerializeField] GameObject MainSelectableHighlights;
        [SerializeField] GameObject ModListItemPrefab;
        [SerializeField] Transform ModListItemContainer;
        [SerializeField] TMP_Text headerText;

        [Header("Selectables")]
        public Selectable Selectable;
        public Selectable AboveSelection;
        public Selectable BelowSelection;

        internal static Vector2 currentSelectedPosition = Vector2.zero;
        List<ListItem> items = new List<ListItem>();
        SearchFilter lastUsedFilter;

        Translation headerTextTranslation = null;

        public void OnRowSelected()
        {
            StartCoroutine(OnSelectFrameDelay());
        }

        /// <summary>
        /// This is a workaround for unity not working when using .Select() inside of OnSelect()
        /// </summary>
        IEnumerator OnSelectFrameDelay()
        {
            yield return null;
            SelectFromPosition(currentSelectedPosition);
        }

        // The position of the selection is determined by another row, informing what the offset
        // of the selection is coming from. Eg if the 3rd item in Row A is selected, when the
        // selection moves down to Row B it needs to tell Row B "we are on the 3rd item" so it can
        // correlate that to a relevant position in the same row, whatever that row's offset is.
        public void SelectFromPosition(Vector2 position)
        {
            // TODO write something to re-select an item when load finished and it's already selected

            if(ErrorPanel.activeSelf)
            {
                // error already active? keep selection
            }
            else if(items.Count == 0)
            {
                // TODO setup an error panel for "zero mods found"
            }
            else
            {
                // iterate over each item and find the one with the closest X position
                ListItem closestItem = null;
                float closestDistance = -1f;
                foreach(ListItem item in items)
                {
                    float distance = Mathf.Abs(position.x - item.transform.position.x);
                    if(closestDistance < 0f || closestDistance > distance)
                    {
                        closestItem = item;
                        closestDistance = distance;
                    }
                }
                if(closestItem == null)
                {
                    Debug.LogError("[mod.io Browser] Attempted to select a null item in ModListRow");
                }
                else
                {
                    InputNavigation.Instance.Select(closestItem.selectable);
                }
            }
        }

        /// <summary>
        /// Use this method to swipe the entire row by a full page (roughly a screen width)
        /// </summary>
        /// <param name="right">the direction of the swipe</param>
        public void SwipeRow(bool right)
        {

            // Rect rect = row.rect;
            // Vector2 v = row.position;

            ListItem listItemToSnapTo = null;
            float posX = 0;
            var width = this.RowPanel.GetComponent<RectTransform>().rect.width;

            // find the left most item that is partially offscreen
            foreach(var item in items)
            {
                if(item.transform is RectTransform rectTransform)
                {
                    float radius = rectTransform.sizeDelta.x / 2f;
                    float offset = this.ModListItemContainer.GetComponent<RectTransform>().anchoredPosition.x;

                    //Anchored position will give the location of the item, then we add to get the right edge or subtract to get the left edge, then we must
                    //add an offset to determine if the current position is visible
                    float edgePosition = right ? rectTransform.anchoredPosition.x + radius + offset : rectTransform.anchoredPosition.x - radius + offset;

                    // Check if this list item is off the left side of the screen
                    if(!right && edgePosition < 0)
                    {
                        // make sure it's closer than any other (if any) item we've found
                        if (edgePosition > posX || posX == 0)
                        {
                            posX = edgePosition;
                            listItemToSnapTo = item;
                        }
                    }
                    else if(right && edgePosition > width)
                    {
                        // make sure it's closer than other (if any) item we've found
                        if (edgePosition < posX || posX == 0)
                        {
                            posX = edgePosition;
                            listItemToSnapTo = item;
                        }
                    }
                }
            }

            listItemToSnapTo?.viewportRestraint?.CheckSelectionHorizontalVisibility();
            InputNavigation.Instance.Select(listItemToSnapTo?.selectable);
        }

        /// <summary>
        /// This method queries the API for the first 20 mods of this filter and applies them to the
        /// row once it has loaded (or not loaded with an error instead)
        /// </summary>
        /// <param name="filter"></param>
        public void AttemptToPopulateRowWithMods(SearchFilter filter)
        {
            SetHeaderText(filter.SortBy);
            lastUsedFilter = filter;
            ErrorPanel.SetActive(false);
            RowPanel.SetActive(false);
            LoadingPanel.SetActive(true);
            MainSelectableHighlights.SetActive(true);
            ModIOUnity.GetMods(filter, GetModsResponse);
        }

        private void SetHeaderText(SortModsBy sortModsBy)
        {
            string header = String.Empty;
            switch(sortModsBy)
            {
                case SortModsBy.Name:
                    header = "Alphabetical";
                    break;
                case SortModsBy.Price:
                    header = "Price";
                    break;
                case SortModsBy.Rating:
                    header = "Highest rated";
                    break;
                case SortModsBy.Popular:
                    header = "Most popular";
                    break;
                case SortModsBy.Downloads:
                    header = "Trending";
                    break;
                case SortModsBy.Subscribers:
                    header = "Most Subscribed";
                    break;
                case SortModsBy.DateSubmitted:
                    header = "Recently added";
                    break;
                default:
                    header = "Unknown Sort Parameter";
                    break;
            }
            headerText.text = header;
            Translation.Get(headerTextTranslation, headerText.text, headerText);
        }

        public void RetryGetMods()
        {
            AttemptToPopulateRowWithMods(lastUsedFilter);
        }

        void GetModsResponse(ResultAnd<ModPage> response)
        {
            if(!Browser.IsOpen)
            {
                return;
            }

            LoadingPanel.SetActive(false);

            if(response.result.Succeeded())
            {
                PopulateRowFromModPage(response.value);
            }
            else
            {
                ErrorPanel.SetActive(true);
            }
        }

        void PopulateRowFromModPage(ModPage page)
        {
            LoadingPanel.SetActive(false);
            ErrorPanel.SetActive(false);
            RowPanel.SetActive(true);
            MainSelectableHighlights.SetActive(false);
            items.Clear();

            // TODO Set the items that can fit on screen
            // TODO

            ListItem lastItem = null;

            foreach(ModProfile mod in page.modProfiles)
            {
                ListItem li = ListItem.GetListItem<HomeModListItem>(ModListItemPrefab, ModListItemContainer, SharedUi.colorScheme);
                li.Setup(mod);
                li.SetViewportRestraint(ModListItemContainer as RectTransform, null);
                Home.Instance.AddModListItemToRowDictionaryCache(li, ModListItemContainer.gameObject);

                // get left nav
                Selectable leftSelectable = null;

                // setup last item onRight navigation to this item
                if(lastItem != null)
                {
                    Navigation lastNavigation = lastItem.selectable.navigation;
                    lastNavigation.selectOnRight = li.selectable;
                    lastItem.selectable.navigation = lastNavigation;

                    leftSelectable = lastItem.selectable;
                }

                // setup navigation
                Navigation navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = AboveSelection,
                    selectOnDown = BelowSelection,
                    selectOnLeft = leftSelectable
                };
                li.selectable.navigation = navigation;

                lastItem = li;

                items.Add(li);
            }
        }

        // TODO sweep left/right methods

        // TODO Add the final item for 'see more' option
    }
}
