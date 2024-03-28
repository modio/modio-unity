using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ModIO;
using ModIO.Util;

namespace ModIOBrowser.Implementation
{
    public class Home : SelfInstancingMonoSingleton<Home>
    {
        [Header("Browse Panel")]
        public GameObject BrowserPanel;
        [SerializeField] Transform BrowserPanelContent;
        [SerializeField] Image BrowserPanelHeaderBackground;
        [SerializeField] Scrollbar BrowserPanelContentScrollBar;
        IEnumerator browserHeaderTransition;
        float browserHeaderLastAlphaTarget = -1;
        Dictionary<GameObject, HashSet<ListItem>> cachedModListItemsByRow = new Dictionary<GameObject, HashSet<ListItem>>();

        [Header("Browse Panel Featured Set")]
        [SerializeField] FeaturedModListItem[] featuredSlotListItems;
        [SerializeField] RectTransform[] featuredSlotPositions;
        [SerializeField] TMP_Text featuredSelectedSubscribeButtonText;
        [SerializeField] Transform featuredSelectedMoreOptionsButtonPosition;
        [SerializeField] GameObject browserFeaturedSlotSelectionHighlightBorder;
        [SerializeField] GameObject browserFeaturedSlotInfo;
        [SerializeField] Image browserFeaturedSlotBackplate;
        [SerializeField] GameObject featuredOptionsButtons;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] ModListRow modListRowPrefab;
        internal bool isFeaturedItemSelected = false;
        ModProfile[] featuredProfiles;
        ModListRow[] modListRows;
        int featuredIndex;

        [Header("Settings")]
        [SerializeField] Selectable browserFeaturedSlotSelection;

        public static bool IsOn() => Instance != null && Instance.BrowserPanel.activeSelf;

        internal Translation featuredSubscribeTranslation = null;

        /// <summary>
        /// This is used internally when a panel wants to change back to the browser/home panel.
        /// This is not used to open the entire UI for the first time, use OpenBrowser() instead.
        /// </summary>
        public void Open()
        {
            Navigating.GoToPanel(Instance.BrowserPanel);
            SelectionManager.Instance.SelectView(UiViews.Browse);
            NavBar.Instance.UpdateNavbarSelection();
        }

        /// <summary>
        /// This is invoked when the current highlighted featured mod gets selected. This will open
        /// the Mod details view and display more information about the specified mod.
        /// </summary>
        public void SelectFeaturedMod()
        {
            if(featuredProfiles == null || featuredProfiles.Length <= featuredIndex)
            {
                return;
            }
            Details.Instance.Open(featuredProfiles[featuredIndex], Open);
        }

        public static void RateMod(ModId modId, ModRating rating)
        {
            if(!Authentication.Instance.IsAuthenticated)
            {
                AuthenticationPanels.Instance.Open();
                return;
            }
            ModIOUnity.RateMod(modId, rating, delegate { });
        }

        /// <summary>
        /// This will open the context menu for the current highlighted featured mod 'more options'
        /// </summary>
        public void OpenMoreOptionsForFeaturedSlot()
        {
            if(Instance.featuredProfiles == null || Instance.featuredProfiles.Length == 0)
            {
                return;
            }

            List<ContextMenuOption> options = new List<ContextMenuOption>();

            // Add Vote up option to context menu
            options.Add(new ContextMenuOption
            {
                nameTranslationReference = "Vote up",
                action = delegate
                {
                    if(Instance.featuredProfiles == null || Instance.featuredProfiles.Length <= Instance.featuredIndex)
                    {
                        return;
                    }
                    RateMod(Instance.featuredProfiles[Instance.featuredIndex].id, ModRating.Positive);
                    ModioContextMenu.Instance.Close();
                }
            });

            // Add Vote up option to context menu
            options.Add(new ContextMenuOption
            {
                nameTranslationReference = "Vote down",
                action = delegate
                {
                    if(Instance.featuredProfiles == null || Instance.featuredProfiles.Length <= Instance.featuredIndex)
                    {
                        return;
                    }
                    RateMod(Instance.featuredProfiles[Instance.featuredIndex].id, ModRating.Positive);
                    ModioContextMenu.Instance.Close();
                }
            });

            // Add Report option to context menu
            options.Add(new ContextMenuOption
            {
                nameTranslationReference = "Report",
                action = delegate
                {
                    ModioContextMenu.Instance.Close();
                    Reporting.Instance.Open(Instance.featuredProfiles[Instance.featuredIndex], browserFeaturedSlotSelection);
                }
            });

            // Open context menu
            ModioContextMenu.Instance.Open(Instance.featuredSelectedMoreOptionsButtonPosition, options, Instance.browserFeaturedSlotSelection);
        }

        /// <summary>
        /// This method is used by the highlighted featured mod's 'Subscribe' button.
        /// This acts as a toggle, and if the mod is already subscribed it will unsubscribe instead.
        /// </summary>
        public void SubscribeToFeaturedMod()
        {
            if(featuredProfiles == null || featuredProfiles.Length <= featuredIndex)
            {
                return;
            }
            if(!Authentication.Instance.IsAuthenticated)
            {
                AuthenticationPanels.Instance?.Open();
                return;
            }


            if(Collection.Instance.IsSubscribed(featuredProfiles[featuredIndex].id))
            {
                // We are pre-emptively changing the text here to make the UI feel more responsive
                //TranslationUpdateable.Get("Subscribe", s => featuredSelectedSubscribeButtonText.text = s);

                Translation.Get(featuredSubscribeTranslation, "Unsubscribe", featuredSelectedSubscribeButtonText);
                Mods.UnsubscribeFromEvent(featuredProfiles[featuredIndex], ()=>UpdateSubscribeButton(featuredProfiles[featuredIndex].id));
            }
            else
            {
                // We are pre-emptively changing the text here to make the UI feel more responsive
                Translation.Get(featuredSubscribeTranslation, "Subscribe", featuredSelectedSubscribeButtonText);
                Mods.SubscribeToEvent(featuredProfiles[featuredIndex], ()=>UpdateSubscribeButton(featuredProfiles[featuredIndex].id));
            }
            RefreshSelectedFeaturedModDetails();
        }

        private void UpdateSubscribeButton(ModId modId)
        {
            UpdateFeaturedSubscribeButtonText(modId);
        }

        /// <summary>
        /// This is used specifically for the main featured carousel at the top of the home view.
        /// This will swipe the current featured selection left and select the next one in the carousel
        /// </summary>
        public void PageFeaturedRow(bool right)
        {
            if(featuredProfiles == null || featuredProfiles.Length == 0)
            {
                // hasn't loaded yet or has no mods
                return;
            }

            if(right)
                featuredIndex = GetNextIndex(featuredIndex, featuredProfiles.Length);
            else
                featuredIndex = GetPreviousIndex(featuredIndex, featuredProfiles.Length);

            FeaturedModListItem.transitionCount = 0;
            foreach(FeaturedModListItem li in featuredSlotListItems)
            {
                int next;
                if(right)
                    next = GetPreviousIndex(li.rowIndex, featuredSlotPositions.Length);
                else
                    next = GetNextIndex(li.rowIndex, featuredSlotPositions.Length);


                // transition the list item to it's next position
                bool isTransitionable;
                if(right)
                    isTransitionable = next != featuredSlotPositions.Length - 1;
                else
                    isTransitionable = next != 0;

                if(isTransitionable)
                {
                    li.Transition(featuredSlotPositions[li.rowIndex], featuredSlotPositions[next]);
                }
                else
                {
                    li.transform.position = featuredSlotPositions[next].position;

                    int change;
                    if(right)
                        change = featuredSlotPositions.Length;
                    else
                        change = featuredSlotPositions.Length * -1;

                    li.profileIndex = GetIndex(li.profileIndex, featuredProfiles.Length, change);
                    li.Setup(featuredProfiles[li.profileIndex]);
                }

                // change list item index
                li.rowIndex = next;
            }
            RefreshSelectedFeaturedModDetails();
        }

        /// <summary>
        /// Hides the highlight around the centered featured mod in the home view carousel
        /// </summary>
        internal void HideFeaturedHighlight()
        {
            browserFeaturedSlotSelectionHighlightBorder.SetActive(false);
            StartCoroutine(ImageTransitions.AlphaFast(browserFeaturedSlotBackplate, 0f));
            browserFeaturedSlotInfo.SetActive(false);
            // browserFeaturedSlotBackplate.gameObject.SetActive(false);
        }

        /// <summary>
        /// Activates the highlight around the centered featured mod in the home view carousel
        /// </summary>
        internal void ShowFeaturedHighlight()
        {
            browserFeaturedSlotSelectionHighlightBorder.SetActive(true);
            StartCoroutine(ImageTransitions.AlphaFast(browserFeaturedSlotBackplate, 1f));
            browserFeaturedSlotBackplate.gameObject.SetActive(true);
            RefreshSelectedFeaturedModDetails();
            browserFeaturedSlotInfo.SetActive(true);
            InputNavigation.Instance.Select(browserFeaturedSlotSelection, true);
        }

        /// <summary>
        /// Updates the text and button details of the centered feature mod, such as name, subscribe
        /// status etc etc
        /// </summary>
        void RefreshSelectedFeaturedModDetails()
        {
            if(featuredProfiles == null || featuredProfiles.Length == 0)
            {
                // hasn't loaded yet or has no mods
                return;
            }

            UpdateFeaturedSubscribeButtonText(featuredProfiles[featuredIndex].id);

            // Some of the featured slots will represent a different mod after the carousel moves
            // because there are 10 featured mods but we only have 5 prefabs displayed at a time,
            // therefore reset their progress pips to their proper profile each time we swipe.
            RefreshFeaturedCarouselProgressTabs();
        }

        /// <summary>
        /// Sets the display of the progress tab in the top right corner of a featured mod's image
        /// (only active on subscribed mods to show 'subscribed', 'downloading' or 'queued')
        /// </summary>
        void RefreshFeaturedCarouselProgressTabs()
        {
            foreach(var mod in featuredSlotListItems)
            {
                mod.progressTab.Setup(featuredProfiles[mod.profileIndex]);
            }
        }

        /// <summary>
        /// Sets the text on the centered feature mod based on it's subscription status
        /// </summary>
        /// <param name="id">the mod id of the mod to check for subscription status</param>
        void UpdateFeaturedSubscribeButtonText(ModId id)
        {
            if(!Collection.Instance.IsPurchased(featuredProfiles[featuredIndex]))
            {
                Translation.Get(featuredSubscribeTranslation, "Buy Now", featuredSelectedSubscribeButtonText);
            }
            else if(Collection.Instance.IsSubscribed(featuredProfiles[featuredIndex].id))
            {
                Translation.Get(featuredSubscribeTranslation, "Unsubscribe", featuredSelectedSubscribeButtonText);
            }
            else
            {
                Translation.Get(featuredSubscribeTranslation, "Subscribe", featuredSelectedSubscribeButtonText);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(featuredSelectedSubscribeButtonText.transform.parent as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(featuredSelectedSubscribeButtonText.transform.parent as RectTransform);
        }

        /// <summary>
        /// We only run this method when the browser is opened, we dont need to refresh the home
        /// view each time we change panels.
        /// </summary>
        internal void RefreshHomePanel()
        {
            ClearRowListItems();
            ClearModListItemRowDictionary();

            // Get Mods for featured row
            ModIOUnity.GetMods(Browser.Instance.FeaturedSearchFilter, AddModProfilesToFeaturedCarousel);

            var filters = Browser.Instance.BrowserRowSearchFilters;
            if(modListRows == null || modListRows.Length == 0)
            {
                modListRows = new ModListRow[filters.Length];
                for(var i = 0; i < filters.Length; i++)
                {
                    ModListRow mlr = Instantiate(this.modListRowPrefab, this.BrowserPanelContent);
                    //move row above the footer in the hierarchy
                    mlr.transform.SetSiblingIndex(BrowserPanelContent.childCount >= 2 ? BrowserPanelContent.childCount-2 : 0);
                    modListRows[i] = mlr;
                    MultiTargetButton mtb = mlr.Selectable.GetComponent<MultiTargetButton>();
                    ViewportRestraint vr = mlr.Selectable.GetComponent<ViewportRestraint>();
                    vr.Viewport = this.GetComponent<RectTransform>();
                    vr.DefaultViewportContainer = this.BrowserPanelContent.GetComponent<RectTransform>();

                    if(i > 0)
                    {
                        var currentRow = modListRows[i];
                        var previousRow = modListRows[i-1];

                        currentRow.AboveSelection = previousRow.Selectable;
                        var n = mtb.navigation;
                        n.selectOnUp = previousRow.Selectable;
                        mtb.navigation = n;

                        previousRow.BelowSelection = currentRow.Selectable;
                        var previousMtb = previousRow.Selectable.GetComponent<MultiTargetButton>();
                        n = previousMtb.navigation;
                        n.selectOnDown = currentRow.Selectable;
                        previousMtb.navigation = n;
                    }
                    else
                    {
                        var n = mtb.navigation;
                        n.selectOnDown = modListRows[0].Selectable;
                        browserFeaturedSlotSelection.navigation = n;

                        modListRows[0].AboveSelection = this.browserFeaturedSlotSelection;

                        n = mtb.navigation;
                        n.selectOnUp = this.browserFeaturedSlotSelection;
                        mtb.navigation = n;
                    }

                }
            }

            if(modListRows.Length != filters.Length)
            {
                Debug.LogError("ModList and filters size should always match");
                return;
            }

            for(var i = 0; i < modListRows.Length; i++)
            {
                modListRows[i].AttemptToPopulateRowWithMods(filters[i]);
            }
        }

        /// <summary>
        /// Hides all of the list items used to display mods in the home view rows.
        /// </summary>
        void ClearRowListItems()
        {
            ListItem.HideListItems<HomeModListItem>();
        }

        /// <summary>
        /// Adds the specified prefab to a row and sets it up as a placeholder to be hydrated later
        /// when we have received a response from the server
        /// </summary>
        /// <param name="list">the parent container of where to instantiate the prefab</param>
        /// <param name="prefab">the prefab to instantiate
        /// (must be enabled and have a ListItem.cs component)</param>
        /// <param name="placeholders">the number of placeholders to create</param>
        /// <typeparam name="T">the type of the prefab. Used to get/cache the correct list item
        /// cache. eg BrowserModListItem</typeparam>
        internal void AddPlaceholdersToList<T>(Transform list, GameObject prefab, int placeholders)
        {
            for(int i = 0; i < placeholders; i++)
            {
                ListItem li = ListItem.GetListItem<T>(prefab, list, SharedUi.colorScheme, true);
                li.PlaceholderSetup();
                li.SetViewportRestraint(SearchResults.Instance.SearchResultsListItemParent as RectTransform, null);
            }
        }

        internal void AddModListItemToRowDictionaryCache(ListItem item, GameObject row)
        {
            // Make sure this row has an entry
            if(!cachedModListItemsByRow.ContainsKey(row))
            {
                cachedModListItemsByRow.Add(row, new HashSet<ListItem>());
            }

            // make sure this item doesnt already have an entry
            if (!cachedModListItemsByRow[row].Contains(item))
            {
                cachedModListItemsByRow[row].Add(item);
            }
        }

        void ClearModListItemRowDictionary()
        {
            cachedModListItemsByRow.Clear();
        }

        // ---------------------------------------------------------------------------------------//
        //                             Callbacks From ModIOUnity                                  //
        //     These are the callbacks we give to ModIOUnity.cs when making a GetMods request     //
        // ---------------------------------------------------------------------------------------//

        /// <summary>
        /// This is used as the callback for GetMods for the featured row
        /// </summary>
        /// <param name="result">whether or not the request succeeded</param>
        /// <param name="modPage">the mods retrieved, if any</param>
        void AddModProfilesToFeaturedCarousel(ResultAnd<ModPage> response)
        {
            if(!Browser.IsOpen)
            {
                return;
            }

            if(!response.result.Succeeded())
            {
                // TODO we need to setup a reattempt option similar to mod list rows
                return;
            }

            featuredProfiles = response.value.modProfiles;
            if(response.value.modProfiles.Length < 10 && response.value.modProfiles.Length > 0)
            {
                featuredProfiles = new ModProfile[10];
                int next = 0;
                for(int i = 0; i < 10; i++)
                {
                    if(next >= response.value.modProfiles.Length)
                    {
                        next = 0;
                    }
                    featuredProfiles[i] = response.value.modProfiles[next];
                    next++;
                }
            }

            if(featuredProfiles.Length < 5)
            {
                // TODO figure out what to do if we dont have enough mods to display
                foreach(var li in featuredSlotListItems)
                {
                    li.PlaceholderSetup();
                }
                return;
            }

            foreach(var li in featuredSlotListItems)
            {
                int index = li.rowIndex;
                if(index >= featuredProfiles.Length)
                {
                    index -= featuredProfiles.Length;
                }
                li.Setup(featuredProfiles[li.profileIndex]);

                // set the viewing index to whichever list item is centered
                // This is just in case someone starts paging left and right before we've retrieved
                // mods to populate the row with.
                if(index == 2)
                {
                    featuredIndex = li.profileIndex;
                }
            }

            RefreshSelectedFeaturedModDetails();
        }

        public void OnScrollValueChange()
        {
            float targetAlpha = -1f;

            // Get the target alpha based on what the scrollbar value is
            if(BrowserPanelContentScrollBar.value < 1f)
            {
                targetAlpha = BrowserPanelHeaderBackground.color.a == 1f ? targetAlpha : 1f;
            }
            else
            {
                targetAlpha = BrowserPanelHeaderBackground.color.a == 0f ? targetAlpha : 0f;
            }

            // If the target alpha needs to change, start the transition coroutine here
            if(targetAlpha != -1f && targetAlpha != browserHeaderLastAlphaTarget)
            {
                browserHeaderLastAlphaTarget = targetAlpha;
                if(browserHeaderTransition != null)
                {
                    StopCoroutine(browserHeaderTransition);
                }
                browserHeaderTransition = ImageTransitions.Alpha(BrowserPanelHeaderBackground, targetAlpha);
                StartCoroutine(browserHeaderTransition);
            }
        }

        public void FeaturedItemSelect(bool state)
        {
            isFeaturedItemSelected = state;
            //featuredOptionsButtons.gameObject.SetActive(state);
        }

        internal static void ModManagementEvent(ModManagementEventType type, ModId id, Result eventResult)
        {
            SubscribedProgressTab.UpdateProgressTab(type, id);
        }

        internal static void UpdateProgressState(ProgressHandle handle)
        {
            if(handle != null && Home.IsOn())
            {
                if(HomeModListItem.listItems.ContainsKey(handle.modId))
                {
                    HomeModListItem.listItems[handle.modId].UpdateProgressBar(handle);
                }

                if(Instance.featuredProfiles != null)
                {
                    foreach(var mod in Instance.featuredSlotListItems)
                    {
                        mod.progressTab.UpdateProgress(handle);
                    }
                }
            }
        }

        public void RefreshModListItems()
        {
            List<SubscribedMod> subbedMods = ModIOUnity.GetSubscribedMods(out var result).ToList();
            if(!result.Succeeded())
            {
                return;
            }

            List<ModProfile> purchasedMods = ModIOUnity.GetPurchasedMods(out result).ToList();
            if(!result.Succeeded())
            {
                return;
            }

            HomeModListItem.listItems.Where(x => x.Value.isActiveAndEnabled)
                              .ToList()
                              .ForEach(x =>
                              {
                                  if(subbedMods.Any(mod => mod.modProfile.Equals(x.Value.profile)) ||
                                     purchasedMods.Any(modProfile => modProfile.Equals(x.Value.profile)))
                                  {
                                      x.Value.Setup(x.Value.profile);
                                  }
                              });
        }

        public void RefreshAllListItems()
        {
            foreach(var listItem in featuredSlotListItems)
            {
                listItem.Refresh();
            }

            foreach(var listItems in cachedModListItemsByRow.Values)
            {
                foreach(var listItem in listItems)
                {
                    listItem.Refresh();
                }
            }
        }

        public void ResetScrollRect()
        {
            scrollRect.verticalNormalizedPosition = 1;
        }

        #region Utility
        public int GetIndex(int current, int length, int change)
        {
            if(length == 0)
            {
                return 0;
            }

            current += change;

            while(current >= length)
            {
                current -= length;
            }
            while(current < 0)
            {
                current += length;
            }

            return current;
        }


        public static int GetPreviousIndex(int current, int length)
        {
            if(length == 0)
            {
                return 0;
            }

            current -= 1;
            if(current < 0)
            {
                current = length - 1;
            }
            return current;
        }

        public static int GetNextIndex(int current, int length)
        {
            if(length == 0)
            {
                return 0;
            }

            current += 1;
            if(current >= length)
            {
                current = 0;
            }
            return current;
        }
        #endregion
    }
}
