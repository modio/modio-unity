using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using ModIO;
using ModIOBrowser.Implementation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    /*  Browser.cs is the main class for handling all of the mod.io UI
     *  It is broken up into the following partial classes for organisation:
     * Browser.cs                   
     * Browser_Authentication.cs    
     * Browser_Collection.cs         
     * Browser_DownloadQueue.cs
     * Browser_ModDetails.cs
     * Browser_Report.cs
     * Browser_SearchPanel.cs
     * Browser_SearchResults.cs
     */
    
    /// <summary>
    /// The main handler for opening and closing the mod IO Browser.
    /// Use Browser.OpenBrowser() to open and Browser.CloseBrowser() to close.
    /// </summary>
    public partial class Browser : MonoBehaviour
    {
        // All of the following fields with [SerializeField] attributes are assigned on the prefab
        // from the unity editor inspector 
        
        [SerializeField] ColorScheme colorScheme;
        [SerializeField] GameObject BrowserCanvas;

        [Header("Browse Panel")]
        [SerializeField] GameObject BrowserPanel;
        [SerializeField] Transform BrowserPanelContent;
        [SerializeField] Transform BrowserPanelRow_RecentlyAdded;
        [SerializeField] Transform BrowserPanelRow_Trending;
        [SerializeField] Transform BrowserPanelRow_MostPopular;
        [SerializeField] Transform BrowserPanelRow_HighestRated;
        [SerializeField] RectTransform BrowserPanelViewport;
        [SerializeField] GameObject BrowserPanelListItem_Regular;
        [SerializeField] GameObject BrowserPanelListItem_Featured;
        [SerializeField] TMP_Text BrowserPanelNavButton;
        [SerializeField] GameObject BrowserPanelNavButtonHighlights;
        [SerializeField] Image BrowserPanelHeaderBackground;
        [SerializeField] Scrollbar BrowserPanelContentScrollBar;
        IEnumerator browserHeaderTransition;
        float browserHeaderLastAlphaTarget = -1;
        Dictionary<GameObject, HashSet<ListItem>> cachedModListItemsByRow = new Dictionary<GameObject, HashSet<ListItem>>();

        [Header("Browse Panel Featured Set")]
        [SerializeField] BrowserFeaturedModListItem[] featuredSlotListItems;
        [SerializeField] RectTransform[] featuredSlotPositions;
        [SerializeField] TMP_Text featuredSelectedName;
        [SerializeField] TMP_Text featuredSelectedSubscribeButtonText;
        [SerializeField] Transform featuredSelectedMoreOptionsButtonPosition;
        internal bool isFeaturedItemSelected = false;
        ModProfile[] featuredProfiles;
        int featuredIndex;

        [Header("Settings")]
        // TODO @Steve these aren't hooked up to all transitions. We may also want a global setting
        // to include viewport adjustments used by the ViewportRestraint.cs class
        [SerializeField] float swipeTransitionTime;
        [SerializeField] AnimationCurve swipeAnimationCurve;
        static int waitingForCallbacks;
        static bool isAuthenticated = false;

        /// <summary>
        /// This is set whenever GotoPanel is invoked, the current opened panel is cached so that
        /// when the user uses 'back' it knows which panel to close and go back to
        /// </summary>
        GameObject currentFocusedPanel;

        [Header("Default Selections")]
        [SerializeField] Selectable defaultBrowserSelection;
        [SerializeField] Selectable defaultModDetailsSelection;
        [SerializeField] Selectable defaultCollectionSelection;

        [Header("Context Menu")]
        public GameObject contextMenu;
        [SerializeField] Transform contextMenuList;
        [SerializeField] GameObject contextMenuListItemPrefab;
        [SerializeField] Selectable contextMenuPreviousSelection;

        [Header("Other Selections")]
        [SerializeField] Selectable browserFeaturedSlotSelection;
        [SerializeField] GameObject browserFeaturedSlotSelectionHighlightBorder;
        
        // Set this when we detect mouse behaviour so we can disable certain controller behaviours
        internal static bool mouseAndKeyboardNavigation = false;

        // globally cached and used to keep track of the current mod management operation progress
        internal ProgressHandle currentModManagementOperationHandle;

        /// This is assigned on OpenBrowser() and will get invoked each time the Browser is closed.
        internal static Action OnClose;

        // Singleton
        internal static Browser Instance;

        // Use Awake() to setup the Singleton for Browser.cs
        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            // Detect mouse outside of context menu to cleanup/close context menu
            if(contextMenu.activeSelf)
            {
                // if we detect a scroll, left or right mouse click, check if mouse is inside context
                // menu bounds. If not, then close context menu
                if(Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1) || Input.GetAxis("Mouse ScrollWheel") != 0f)
                {
                    // check if the mouse is within the bounds of the contextMenu
                    RectTransform contextRect = contextMenu.transform as RectTransform;
                    Vector3 mousePositionLocalToRect = contextRect.InverseTransformPoint(Input.mousePosition);

                    if(!contextRect.rect.Contains(mousePositionLocalToRect))
                    {
                        contextMenu.SetActive(false);
                        // if using mouse we dont close using the method CloseContextMenu() because
                        // we dont want to move the selection
                        // CloseContextMenu();
                    }
                }
            }
        }

#region Frontend methods
        /// <summary>
        /// Use this method to open the Mod Browser UI for a user to begin browsing.
        /// </summary>
        /// <param name="onClose">Assign an action to be invoked when the browser is closed</param>
        /// <remarks>
        /// Keep in mind that a user may close the browser from an internal method, such as using
        /// the UI 'back' button or pressing ESC.
        /// </remarks>
        public static void OpenBrowser([CanBeNull] Action onClose)
        {
            if(!ModIOUnity.IsInitialised())
            {
                // TODO Log error for not being initialized
                Debug.LogWarning("[mod.io Browser] Could not open because the ModIO plugin"
                                 + " hasn't been initialised");
                return;
            }

            OnClose = onClose;

            ModIOUnity.IsAuthenticated((r) =>
            {
                if(r.Succeeded())
                {
                    isAuthenticated = true;
                    ModIOUnity.FetchUpdates(delegate { });
                }
                else
                {
                    isAuthenticated = false;
                }
            });

            if(Instance == null)
            {
                Debug.LogWarning("[mod.io Browser] Could not open because the Browser.cs"
                                 + " singleton hasn't been set yet. (Check the gameObject holding"
                                 + " the Browser.cs component isn't set to inactive)");
                // REVIEW @Jackson the Logger is internal, should we use debug? make a separate
                // logger for the browser?
                // Logger.Log(LogLevel.Error, "[modio-browser] The Browser singleton hasnt been "
                //                            + "initialized yet. Make sure this method is not being "
                //                            + "called inside Awake(), consider using Start() instead.");
                return;
            }

            // Activate the Canvas
            if(!Instance.BrowserCanvas.activeSelf)
            {
                Instance.BrowserCanvas.SetActive(true);
            }

            Instance.CacheLocalSubscribedModStatuses();
            Instance.SetupUserAvatar();
            Instance.OpenBrowserPanel();
            Instance.RefreshBrowserPanel();

            Result result = ModIOUnity.EnableModManagement(Instance.ModManagementEvent);
        }

        /// <summary>
        /// Use this method to properly close and hide the Mod Browser UI.
        /// </summary>
        /// <remarks>
        /// You may not need to use this method since the browser has the ability to close itself
        /// </remarks>
        public static void CloseBrowser()
        {
            if(Instance == null)
            {
                // REVIEW @Jackson see above
                return;
            }

            // Deactivate the Canvas
            Instance.BrowserCanvas.SetActive(false);
            OnClose?.Invoke();
        }
#endregion // Frontend methods

#region Misc
        /// <summary>
        /// This is used to get the installed and subscribed mods and cache them for use across the UI
        /// </summary>
        internal void CacheLocalSubscribedModStatuses()
        {
            // Get Subscribed Mods
            SubscribedMod[] subs = ModIOUnity.GetSubscribedMods(out Result result);
            if(subs == null)
            {
                subs = new SubscribedMod[0];
            }
            subscribedMods = subs;

            // Get Installed Mods
            InstalledMod[] installs = ModIOUnity.GetSystemInstalledMods(out result);
            if(result.Succeeded())
            {
                installedMods = installs;
            }
        }

        internal static bool IsSubscribed(ModId id)
        {
            return IsSubscribed(id, out SubscribedModStatus status);
        }

        /// <summary>
        /// This will check if the given modId is subscribed and will also return the status of the
        /// specified mod, such as SubscribedModStatus.Installed
        /// </summary>
        /// <param name="id">The id of the mod to check</param>
        /// <param name="status">the out status of given modId</param>
        /// <returns>true if the mod is subscribed</returns>
        internal static bool IsSubscribed(ModId id, out SubscribedModStatus status)
        {
            if(Instance.subscribedMods == null)
            {
                Instance.CacheLocalSubscribedModStatuses();
            }

            foreach(var m in Instance.subscribedMods)
            {
                if(m.modProfile.id == id)
                {
                    status = m.status;
                    return true;
                }
            }
            foreach(var m in Instance.pendingSubscriptions)
            {
                if(m.id == id)
                {
                    status = SubscribedModStatus.WaitingToDownload;
                    return true;
                }
            }

            status = SubscribedModStatus.None;
            return false;
        }

        internal static bool IsInstalled(ModId id)
        {
            if(Instance.installedMods == null)
            {
                Instance.CacheLocalSubscribedModStatuses();
            }

            foreach(var m in Instance.installedMods)
            {
                if(m.modProfile.id == id)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool GetSubscribedProfile(ModId id, out ModProfile profile)
        {
            if(Instance.subscribedMods == null)
            {
                Instance.CacheLocalSubscribedModStatuses();
            }

            foreach(var m in Instance.subscribedMods)
            {
                if(m.modProfile.id == id)
                {
                    profile = m.modProfile;
                    return true;
                }
            }
            foreach(var m in Instance.pendingSubscriptions)
            {
                if(m.id == id)
                {
                    profile =  m;
                    return true;
                }
            }

            profile = default;
            return false;
        }

        /// <summary>
        /// This works the same as UnsubscribeFromModEvent() but it subscribes instead.
        /// Always use this method to subscribe to mods so that we can globally track the result and
        /// make updates where necessary.
        /// Eg update the mod collection list or 'subscribed' pips on mod list items displayed in
        /// the home view.
        /// </summary>
        /// <param name="profile">the mod being subscribed to</param>
        /// <param name="callback">any extra callback to run once the response is received</param>
        internal static void SubscribeToModEvent(ModProfile profile, Action callback = null)
        {
            if(!isAuthenticated)
            {
                Instance?.OpenAuthenticationPanel();
                return;
            }
            
            Instance.pendingSubscriptions.Add(profile);

            ModIOUnity.SubscribeToMod(profile.id,
                delegate(Result result)
                {
                    Instance.pendingSubscriptions.Remove(profile);

                    if(result.Succeeded())
                    {
                        Instance.CacheLocalSubscribedModStatuses();

                        // if collection open, make another list item for the new subscribed item
                        if(Instance.CollectionPanel.activeSelf)
                        {
                            Instance.RefreshCollectionListItems();
                        }
                    }

                    callback?.Invoke();
                });
        }

        /// <summary>
        /// This works the same as SubscribeToModEvent() but it unsubscribes instead.
        /// Always use this method to unsubscribe from mods so that we can globally track the result and
        /// make updates where necessary.
        /// Eg update the mod collection list or 'subscribed' pips on mod list items displayed in
        /// the home view.
        /// </summary>
        /// <param name="profile">the mod to unsubscribe</param>
        /// <param name="callback">any extra callback to run once the response is received</param>
        internal static void UnsubscribeFromModEvent(ModProfile profile, Action callback = null)
        {
            if(!isAuthenticated)
            {
                return;
            }
            
            Instance.pendingSubscriptions.Remove(profile);
            if(!Instance.pendingUnsubscribes.Contains(profile.id))
            {
                Instance.pendingUnsubscribes.Add(profile.id);
            }

            ModIOUnity.UnsubscribeFromMod(profile.id,
                delegate(Result result)
                {
                    if(Instance.pendingUnsubscribes.Contains(profile.id))
                    {
                        Instance.pendingUnsubscribes.Remove(profile.id);
                    }
                    if(result.Succeeded())
                    {
                        Instance.CacheLocalSubscribedModStatuses();
                    }

                    callback?.Invoke();
                });
        }

        /// <summary>
        /// Always use this method to when we want to rate a mod so that when we receive a result we
        /// can update any UI accordingly.
        /// Eg update the mod details Rate up/down buttons to show that you've rated this mod
        /// </summary>
        /// <param name="modId">the mod id to rate</param>
        /// <param name="rating">the rating to apply, eg ModRating.Positive</param>
        /// <param name="callback">any extra callback to run once the response is received</param>
        internal static void RateModEvent(ModId modId, ModRating rating, Action callback = null)
        {
            if(!isAuthenticated)
            {
                return;
            }
            
            ModIOUnity.RateMod(modId, rating, delegate(Result result)
            {
                callback?.Invoke();
            });
        }

        /// <summary>
        /// This is simply an On/Off state of the collection/browser buttons at the top of the UI
        /// panel to go between the two corresponding menus. The display is based on which menu you
        /// are currently in.
        /// </summary>
        internal void UpdateNavbarSelection()
        {
            if(CollectionPanel.activeSelf)
            {
                Color col = CollectionPanelNavButton.color;
                col.a = 1f;
                CollectionPanelNavButton.color = col;
                CollectionPanelNavButtonHighlights.SetActive(true);

                col = BrowserPanelNavButton.color;
                col.a = 0.5f;
                BrowserPanelNavButton.color = col;
                BrowserPanelNavButtonHighlights.SetActive(false);
            }
            else
            {
                Color col = CollectionPanelNavButton.color;
                col.a = 0.5f;
                CollectionPanelNavButton.color = col;
                CollectionPanelNavButtonHighlights.SetActive(false);

                col = BrowserPanelNavButton.color;
                col.a = 1f;
                BrowserPanelNavButton.color = col;
                BrowserPanelNavButtonHighlights.SetActive(true);
            }
        }
        
        /// <summary>
        /// This is a generic method for transitioning the alpha of an image component. Currently
        /// being used to show/hide the headers that fade in/out as you scroll dup/down the page
        /// in Collection view and Search results view.
        /// </summary>
        /// <param name="image">component to transition color alpha</param>
        /// <param name="targetAlphaValue">the target alpha to end transition at</param>
        /// <returns></returns>
        IEnumerator TransitionImageAlpha(Image image, float targetAlphaValue)
        {
            float incrementSize = 0.05f;
            Color color = image.color;
            while(color.a != targetAlphaValue)
            {
                color.a = color.a > targetAlphaValue ? color.a - incrementSize : color.a + incrementSize;
                
                // make sure we dont go outside the bounds
                if(color.a < 0f || color.a > 1f)
                {
                    color.a = targetAlphaValue;
                }

                image.color = color;
                    
                yield return new WaitForSecondsRealtime(0.025f);
            }
        }

        /// <summary>
        /// This updates the display of the bumper icons in the search panel, showing whether or not
        /// you can continue jumping to the next tag category with the specified bumper input.
        /// </summary>
        internal void UpdateSearchPanelBumperIcons()
        {
            Color left = SearchPanelLeftBumperIcon.color;
            left.a = TagJumpToSelection.CanTabLeft() ? 1f : 0.2f;
            SearchPanelLeftBumperIcon.color = left;

            Color right = SearchPanelRightBumperIcon.color;
            right.a = TagJumpToSelection.CanTabRight() ? 1f : 0.2f;
            SearchPanelRightBumperIcon.color = right;
        }

        public void SetToControllerNavigation()
        {
            mouseAndKeyboardNavigation = false;
        }

        public void SetToMouseAndKeyboardNavigation()
        {
            mouseAndKeyboardNavigation = true;
        }

#endregion // Misc

#region Input Actions and Behaviour
        // The following methods are invoked from InputReceiver.cs which acts mostly as a
        // pass-through for the user to inform the browser of certain inputs.
        internal static void Cancel()
        {
            if(Instance.contextMenu.activeSelf)
            {
                Instance.CloseContextMenu();
            }
            else if(MultiTargetDropdown.currentMultiTargetDropdown != null)
            {
                MultiTargetDropdown.currentMultiTargetDropdown.Hide();
            }
            else if(Instance.SearchPanel.activeSelf)
            {
                Instance.CloseSearchPanel();
            }
            else if(Instance.AuthenticationPanel.activeSelf)
            {
                Instance.CloseAuthenticationPanel();
            }
            else if(Instance.DownloadQueuePanel.activeSelf)
            {
                Instance.ToggleDownloadQueuePanel();
            }
            else if(Instance.ModDetailsPanel.activeSelf)
            {
                Instance.CloseModDetailsPanel();
            }
            else if(Instance.currentFocusedPanel != Instance.BrowserPanel)
            {
                Instance.OpenBrowserPanel();
            }
            else
            {
                CloseBrowser();
            }
        }

        internal static void Alternate()
        {
            if(Instance.SearchPanel.activeSelf)
            {
                // Apply filters and show results
                Instance.ApplySearchFilter();
            }
            else if(Instance.BrowserPanel.activeSelf)
            {
                // if overlay browser inflated, subscribe/unsubscribe
                // if featured item highlighted/selected subscribe/unsubscribe
                if(!SelectionOverlayHandler.TryAlternateForBrowserOverlayObject() && Instance.isFeaturedItemSelected)
                {
                    Instance.SubscibeToFeaturedMod();
                }
            }
            else if(Instance.ModDetailsPanel.activeSelf)
            {
                // if mod details panel selected subscribe/unsubscribe
                Instance.ModDetailsSubscribeButtonPress();
            }
            else if(Instance.CollectionPanel.activeSelf)
            {
                if(Instance.currentSelectedCollectionListItem != null)
                {
                    Instance.currentSelectedCollectionListItem.UnsubscribeButton();
                }
            }
            else if(Instance.SearchResultsPanel.activeSelf)
            {
                // if search results list item selected subscribe/unsubscribe}
                SelectionOverlayHandler.TryAlternateForSearchResultsOverlayObject();
            }
        }

        internal static void Options()
        {
            // if browser overlay inflated, bring up more options
            // if browser featured selected bring up more options
            if(Instance.contextMenu.activeSelf)
            {
                Instance.CloseContextMenu();
            }
            else if(Instance.BrowserPanel.activeSelf)
            {
                // if overlay browser inflated, subscribe/unsubscribe
                // if featured item highlighted/selected subscribe/unsubscribe
                if(!SelectionOverlayHandler.TryToOpenMoreOptionsForBrowserOverlayObject() && Instance.isFeaturedItemSelected)
                {
                    Instance.OpenMoreOptionsForFeaturedSlot();
                }
            }
            else if(Instance.CollectionPanel.activeSelf)
            {
                if(Instance.currentSelectedCollectionListItem != null)
                {
                    Instance.currentSelectedCollectionListItem.ShowMoreOptions();
                }
            }
            else if(Instance.SearchResultsPanel.activeSelf)
            {
                // if search results list item selected subscribe/unsubscribe}
                SelectionOverlayHandler.TryToOpenMoreOptionsForSearchResultsOverlayObject();
            }
            else if(Instance.SearchPanel.activeSelf)
            {
                // clear filter results
                searchFilterTags.Clear();
                Instance.SearchPanelField.text = "";
                Instance.SetupSearchPanelTags();
            }
        }

        internal static void TabLeft()
        {
            if(Instance.SearchPanel.activeSelf)
            {
                TagJumpToSelection.GoToPreviousSelection();
            }
            else if(Instance.ModDetailsPanel.activeSelf)
            {
                Instance.ShowPreviousGalleryImage();
            }
            else if(Instance.BrowserPanel.activeSelf || Instance.CollectionPanel.activeSelf)
            {
                ToggleBetweenBrowserAndCollection();
            }
        }

        internal static void TabRight()
        {
            if(Instance.SearchPanel.activeSelf)
            {
                TagJumpToSelection.GoToNextSelection();
            }
            else if(Instance.ModDetailsPanel.activeSelf)
            {
                Instance.ShowNextGalleryImage();
            }
            else if(Instance.BrowserPanel.activeSelf || Instance.CollectionPanel.activeSelf)
            {
                ToggleBetweenBrowserAndCollection();
            }
        }

        internal static void SearchInput()
        {
            if(Instance.SearchPanel.activeSelf)
            {
                Instance.CloseSearchPanel();
            }
            else
            {
                Instance.OpenSearchPanel();
            }
        }

        internal static void MenuInput()
        {
            Instance?.OpenMenuProfile();
        }

        internal static void Scroll(float direction)
        {
            if(Instance.ModDetailsPanel.activeSelf && !Instance.ReportPanel.activeSelf)
            {
                Vector3 position = Instance.ModDetailsContentRect.position;
                position.y += direction * (100f * Time.fixedDeltaTime) * -1f;
                Instance.ModDetailsContentRect.position = position;
            }
        }

#endregion //  Input Actions and Behaviour

#region Generic Navigation for panels
        /// <summary>
        /// Closes all panels and opens the specified one.
        /// </summary>
        /// <param name="panel">the new GameObject UI panel to enable</param>
        internal static void GoToPanel(GameObject panel)
        {
            // Ensure no other panels are open
            CloseAll();

            // Open the specified panel
            panel.SetActive(true);

            Instance.currentFocusedPanel = panel;
        }

        /// <summary>
        /// This is a force close of all UI panels and gameObjects. This ensures everything gets
        /// deactivated and should be used before we wish to open a different fullscreen panel
        /// </summary>
        internal static void CloseAll()
        {
            // (Note):This may seem verbose but we want to check if a panel is already active before
            // using SetActive(false) because it will dirty the entire canvas unnecessarily when we
            // try to deactivate an inactive object instead of ignoring it.
            if(Instance.BrowserPanel.activeSelf)
            {
                Instance.BrowserPanel.SetActive(false);
            }
            if(Instance.CollectionPanel.activeSelf)
            {
                Instance.CollectionPanel.SetActive(false);
            }
            if(Instance.ModDetailsPanel.activeSelf)
            {
                Instance.ModDetailsPanel.SetActive(false);
            }
            if(Instance.SearchPanel.activeSelf)
            {
                Instance.SearchPanel.SetActive(false);
            }
            if(Instance.SearchResultsPanel.activeSelf)
            {
                Instance.SearchResultsPanel.SetActive(false);
            }
            if(Instance.AuthenticationPanel.activeSelf)
            {
                Instance.AuthenticationPanel.SetActive(false);
            }
            if(Instance.DownloadQueuePanel.activeSelf)
            {
                Instance.DownloadQueuePanel.SetActive(false);
            }
            if(Instance.contextMenu.activeSelf)
            {
                Instance.contextMenu.SetActive(false);
            }
            if(Instance.ReportPanel.activeSelf)
            {
                Instance.ReportPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Depending on the authentication status this will either open the download history panel
        /// or the authentication wizard dialog.
        /// </summary>
        public void OpenMenuProfile()
        {
            if(!isAuthenticated)
            {
                Instance.OpenAuthenticationPanel();
            }
            else
            {
                Instance.ToggleDownloadQueuePanel();
            }
        }

        /// <summary>
        /// This method is used when a left or right bumper is pressed and the view needs to change
        /// from the home view to the collection view and vice versa.
        /// </summary>
        static void ToggleBetweenBrowserAndCollection()
        {
            if(Instance.BrowserPanel.activeSelf)
            {
                Instance.OpenModCollection();
            }
            else
            {
                Instance.OpenBrowserPanel();
            }
        }
#endregion // Generic Navigation for panels

#region Browser Panel
        // These are all of the internal methods that pertain to managing the main home view panel
        // as well as populating it, eg the Featured row and other rows, recently added, most popular
        // etc etc
        
        /// <summary>
        /// This is used internally when a panel wants to change back to the browser/home panel.
        /// This is not used to open the entire UI for the first time, use OpenBrowser() instead.
        /// </summary>
        public void OpenBrowserPanel()
        {
            GoToPanel(Instance.BrowserPanel);
            Instance.defaultBrowserSelection.Select();
            UpdateNavbarSelection();
        }

        /// <summary>
        /// We need this to be able to hook up a 'back' button in the UI to close the browser.
        /// The CloseBrowser method for the frontend use is static.
        /// </summary>
        public void CloseBrowserPanel()
        {
            CloseBrowser();
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
            OpenModDetailsPanel(featuredProfiles[featuredIndex], OpenBrowserPanel);
        }

        /// <summary>
        /// This will open the context menu for the current highlighted featured mod 'more options'
        /// </summary>
        public void OpenMoreOptionsForFeaturedSlot()
        {
            List<ContextMenuOption> options = new List<ContextMenuOption>();

            // Add Vote up option to context menu
            options.Add(new ContextMenuOption
            {
                name = "Vote up",
                action = delegate
                {
                    if(Instance.featuredProfiles == null || Instance.featuredProfiles.Length <= Instance.featuredIndex)
                    {
                        return;
                    }
                    ModIOUnity.RateMod(Instance.featuredProfiles[Instance.featuredIndex].id, ModRating.Positive, delegate { });
                    Instance.CloseContextMenu();
                }
            });

            // Add Vote up option to context menu
            options.Add(new ContextMenuOption
            {
                name = "Vote down",
                action = delegate
                {
                    if(Instance.featuredProfiles == null || Instance.featuredProfiles.Length <= Instance.featuredIndex)
                    {
                        return;
                    }
                    ModIOUnity.RateMod(Instance.featuredProfiles[Instance.featuredIndex].id, ModRating.Negative, delegate { });
                    Instance.CloseContextMenu();
                }
            });

            // Add Report option to context menu
            options.Add(new ContextMenuOption
            {
                name = "Report",
                action = delegate
                {
                    Debug.Log("report");
                    Instance.CloseContextMenu();
                    Instance.OpenReportPanel(Instance.featuredProfiles[Instance.featuredIndex], browserFeaturedSlotSelection);
                }
            });

            // Open context menu
            Instance.OpenContextMenu(Instance.featuredSelectedMoreOptionsButtonPosition, options, Instance.browserFeaturedSlotSelection);
        }

        /// <summary>
        /// This method is used by the highlighted featured mod's 'Subscribe' button.
        /// This acts as a toggle, and if the mod is already subscribed it will unsubscribe instead.
        /// </summary>
        public void SubscibeToFeaturedMod()
        {
            if(featuredProfiles == null || featuredProfiles.Length <= featuredIndex)
            {
                return;
            }
            if(IsSubscribed(featuredProfiles[featuredIndex].id))
            {
                // We are pre-emptively changing the text here to make the UI feel more responsive
                featuredSelectedSubscribeButtonText.text = "Subscribe";
                UnsubscribeFromModEvent(featuredProfiles[featuredIndex],
                    delegate { UpdateFeaturedSubscribeButtonText(featuredProfiles[featuredIndex].id); });
            }
            else
            {
                // We are pre-emptively changing the text here to make the UI feel more responsive
                featuredSelectedSubscribeButtonText.text = "Unsubscribe";
                SubscribeToModEvent(featuredProfiles[featuredIndex],
                    delegate { UpdateFeaturedSubscribeButtonText(featuredProfiles[featuredIndex].id); });
            }
            RefreshSelectedFeaturedModDetails();
        }

        /// <summary>
        /// This will swipe the entire row one full screen width to the right. If there is no more
        /// width to display it will line up the left edge of the transform to the left edge of the
        /// screen.
        /// </summary>
        /// <param name="row">the RectTransform of the gameObject to be moved</param>
        public void PageRowLeft(RectTransform row)
        {
            // Rect rect = row.rect;
            // Vector2 v = row.position;

            ListItem listItemToSnapTo = null;
            float posX = 0;
            
            // find the left most item that is partially offscreen
            foreach(var item in cachedModListItemsByRow[row.gameObject])
            {
                if(item.transform is RectTransform rectTransform)
                {
                    float radius = rectTransform.sizeDelta.x / 2f;
                    float leftSidePositionX = rectTransform.position.x - radius;
                    
                    // Check if this list item is off the left side of the screen
                    if(leftSidePositionX < 0)
                    {
                        // make sure it's closer than any other (if any) item we've found
                        if (leftSidePositionX > posX || posX == 0)
                        {
                            posX = leftSidePositionX;
                            listItemToSnapTo = item;
                        }
                    }
                }
            }

            listItemToSnapTo?.viewportRestraint?.CheckSelectionHorizontalVisibility();
            listItemToSnapTo?.selectable?.Select();
        }
        
        /// <summary>
        /// This will swipe the entire row one full screen width to the left. If there is no more
        /// width to display it will line up the right edge of the transform to the right edge of
        /// the screen.
        /// </summary>
        /// <param name="row">the RectTransform of the gameObject to be moved</param>
        public void PageRowRight(RectTransform row)
        {
            ListItem listItemToSnapTo = null;
            float posX = 0;
            
            // find the left most item that is partially offscreen
            foreach(var item in cachedModListItemsByRow[row.gameObject])
            {
                if(item.transform is RectTransform rectTransform)
                {
                    float radius = rectTransform.sizeDelta.x / 2f;
                    float rightSidePositionX = rectTransform.position.x + radius;
                    
                    // Check if this list item is off the left side of the screen
                    if(rightSidePositionX > Screen.width)
                    {
                        // make sure it's closer than other (if any) item we've found
                        if (rightSidePositionX < posX || posX == 0)
                        {
                            posX = rightSidePositionX;
                            listItemToSnapTo = item;
                        }
                    }
                }
            }

            listItemToSnapTo?.viewportRestraint?.CheckSelectionHorizontalVisibility();
            listItemToSnapTo?.selectable?.Select();
        }

        /// <summary>
        /// This is used specifically for the main featured carousel at the top of the home view.
        /// This will swipe the current featured selection left and select the next one in the carousel
        /// (to the right)
        /// </summary>
        public void PageFeaturedRowLeft()
        {
            if(featuredProfiles == null)
            {
                // hasn't loaded yet
                return;
            }
            featuredIndex = Utility.GetPreviousIndex(featuredIndex, featuredProfiles.Length);

            BrowserFeaturedModListItem.transitionCount = 0;
            foreach(BrowserFeaturedModListItem li in featuredSlotListItems)
            {
                int next = Utility.GetNextIndex(li.rowIndex, featuredSlotPositions.Length);

                // transition the list item to it's next position
                if(next != 0)
                {
                    li.Transition(featuredSlotPositions[li.rowIndex], featuredSlotPositions[next]);
                }
                else
                {
                    li.transform.position = featuredSlotPositions[next].position;
                    li.profileIndex = Utility.GetIndex(li.profileIndex, featuredProfiles.Length, featuredSlotPositions.Length * -1);
                    li.Setup(featuredProfiles[li.profileIndex]);
                }

                // change list item index
                li.rowIndex = next;
            }
            RefreshSelectedFeaturedModDetails();
        }

        /// <summary>
        /// This is used specifically for the main featured carousel at the top of the home view.
        /// This will swipe the current featured selection right and select the previous one in the
        /// carousel (to the left)
        /// </summary>
        public void PageFeaturedRowRight()
        {
            if(featuredProfiles == null)
            {
                // hasn't loaded yet
                return;
            }
            featuredIndex = Utility.GetNextIndex(featuredIndex, featuredProfiles.Length);

            BrowserFeaturedModListItem.transitionCount = 0;
            foreach(BrowserFeaturedModListItem li in featuredSlotListItems)
            {
                int next = Utility.GetPreviousIndex(li.rowIndex, featuredSlotPositions.Length);

                // transition the list item to it's next position
                if(next != featuredSlotPositions.Length - 1)
                {
                    li.Transition(featuredSlotPositions[li.rowIndex], featuredSlotPositions[next]);
                }
                else
                {
                    li.transform.position = featuredSlotPositions[next].position;
                    li.profileIndex = Utility.GetIndex(li.profileIndex, featuredProfiles.Length, featuredSlotPositions.Length);
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
        }

        /// <summary>
        /// Activates the highlight around the centered featured mod in the home view carousel
        /// </summary>
        internal void ShowFeaturedHighlight()
        {
            browserFeaturedSlotSelectionHighlightBorder.SetActive(true);
        }

        /// <summary>
        /// Updates the text and button details of the centered feature mod, such as name, subscribe
        /// status etc etc
        /// </summary>
        internal void RefreshSelectedFeaturedModDetails()
        {
            featuredSelectedName.text = featuredProfiles[featuredIndex].name;
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
        internal void RefreshFeaturedCarouselProgressTabs()
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
            if(IsSubscribed(id))
            {
                featuredSelectedSubscribeButtonText.text = "Unsubscribe";
            }
            else
            {
                featuredSelectedSubscribeButtonText.text = "Subscribe";
            }
        }

        /// <summary>
        /// We only run this method when the browser is opened, we dont need to refresh the home
        /// view each time we change panels.
        /// </summary>
        internal void RefreshBrowserPanel()
        {
            ClearRowListItems();
            ClearModListItemRowDictionary();

            // Setup filter
            SearchFilter filter = new SearchFilter();
            filter.SetPageIndex(0);
            filter.SetPageSize(10);
            filter.SortBy(SortModsBy.Downloads); // TODO How do we get top ten featured?
            // Note: this is a mistake on the backend api. Ascending is swapped with descending for this field
            filter.SetToAscending(true);

            // Get Mods for featured row
            waitingForCallbacks++;
            ModIOUnity.GetMods(filter, AddModProfilesToFeaturedCarousel);

            // Edit filter for next row
            filter.SetPageSize(20);
            filter.SortBy(SortModsBy.DateSubmitted);
            filter.SetToAscending(false);

            // Get Mods for Recently Added row
            waitingForCallbacks++;
            AddPlaceholdersToList<BrowserModListItem>(BrowserPanelRow_RecentlyAdded,
                BrowserPanelListItem_Regular, 20);
            ModIOUnity.GetMods(filter, (result, mods) => { AssignModsToRow(result, mods, BrowserPanelRow_RecentlyAdded);});

            // Edit filter for next row
            filter.SortBy(SortModsBy.Subscribers);
            filter.SetToAscending(true);

            // Get Mods for Trending row
            waitingForCallbacks++;
            AddPlaceholdersToList<BrowserModListItem>(BrowserPanelRow_Trending,
                BrowserPanelListItem_Regular, 20);
            ModIOUnity.GetMods(filter, (result, mods) => { AssignModsToRow(result, mods, BrowserPanelRow_Trending);});

            // Edit filter for next row
            filter.SortBy(SortModsBy.Popular);
            filter.SetToAscending(false);

            // Get Mods for Most Popular row
            waitingForCallbacks++;
            AddPlaceholdersToList<BrowserModListItem>(BrowserPanelRow_MostPopular,
                BrowserPanelListItem_Regular, 20);
            ModIOUnity.GetMods(filter, (result, mods) => { AssignModsToRow(result, mods, BrowserPanelRow_MostPopular);});

            // Edit filter for next row
            filter.SortBy(SortModsBy.Rating);
            filter.SetToAscending(true);

            // Get Mods for highest rated row
            waitingForCallbacks++;
            AddPlaceholdersToList<BrowserModListItem>(BrowserPanelRow_HighestRated,
                BrowserPanelListItem_Regular, 20);
            ModIOUnity.GetMods(filter, (result, mods) => { AssignModsToRow(result, mods, BrowserPanelRow_HighestRated);});
        }

        /// <summary>
        /// Hides all of the list items used to display mods in the home view rows. 
        /// </summary>
        void ClearRowListItems()
        {
            ListItem.HideListItems<BrowserModListItem>();
        }

        /// <summary>
        /// Hides any remaining placeholder list items. Used after a successful get mods request
        /// returns and we want to cleanup any placeholder items we no longer need.
        /// </summary>
        void ClearPlaceholderListItems()
        {
            ListItem.HideListItems<BrowserModListItem>(true);
            //ListItem.HideListItems<BrowserFeaturedModListItem>(true);
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
        void AddPlaceholdersToList<T>(Transform list, GameObject prefab, int placeholders)
        {
            for(int i = 0; i < placeholders; i++)
            {
                ListItem li = ListItem.GetListItem<T>(prefab, list, colorScheme, true);
                li.PlaceholderSetup();
            }
        }

        /// <summary>
        /// When given a collection of mod profiles, this will populate the specified row with a
        /// list item for each of the given profiles. It will use placeholder items if any active
        /// are found.
        /// </summary>
        /// <param name="mods">the array of ModProfiles to populate the row with</param>
        /// <param name="listItemPrefab">the prefab to use as a list item</param>
        /// <param name="row">the parent/row of where the prefabs go</param>
        void AddModProfilesToRow(ModProfile[] mods, GameObject listItemPrefab, Transform row)
        {
            foreach(ModProfile mod in mods)
            {
                ListItem li = ListItem.GetListItem<BrowserModListItem>(listItemPrefab, row, colorScheme);
                li.Setup(mod);
                li.SetViewportRestraint(BrowserPanelContent as RectTransform, null);
                AddModListItemToRowDictionaryCache(li, row.gameObject);
            }
        }

        void AddModListItemToRowDictionaryCache(ListItem item, GameObject row)
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
        void AddModProfilesToFeaturedCarousel(Result result, ModPage modPage)
        {
            waitingForCallbacks--;
            if(!result.Succeeded())
            {
                return;
            }

            featuredProfiles = modPage.modProfiles;

            if(featuredProfiles.Length < 5)
            {
                // TODO figure out what to do if we dont have enough mods to display
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

        /// <summary>
        /// This is used as the callback for GetMods for all of the mod rows in home view
        /// </summary>
        /// <param name="result">whether or not the request succeeded</param>
        /// <param name="modPage">the mods retrieved, if any</param>
        /// <param name="row">the row that we will assign the mods to</param>
        void AssignModsToRow(Result result, ModPage modPage, Transform row)
        {
            waitingForCallbacks--;
            if(!result.Succeeded())
            {
                return;
            }

            AddModProfilesToRow(modPage.modProfiles, BrowserPanelListItem_Regular, row);

            if(waitingForCallbacks <= 0)
            {
                ClearPlaceholderListItems();
            }
        }
        
        public void BrowserPanelOnScrollValueChange()
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
                browserHeaderTransition = TransitionImageAlpha(BrowserPanelHeaderBackground, targetAlpha);
                StartCoroutine(browserHeaderTransition);
            }
        }
#endregion // Browser Panel

#region Context Menu

        /// <summary>
        /// This opens a context menu with the specified options 'options' and makes itself a child
        /// to the given transform so that it can be assigned to remain in front of an element.
        /// It will also use the parent as the origin of where the menu will spawn from.
        /// The pivot is (0.5f, 0f) x, y
        /// </summary>
        /// <param name="t"></param>
        /// <param name="options"></param>
        /// <param name="previousSelection"></param>
        internal void OpenContextMenu(Transform t, List<ContextMenuOption> options, Selectable previousSelection)
        {
            if(options.Count < 1)
            {
                // We can't open a context menu without any context options
                return;
            }

            Vector2 position = t.position;

            // This counteracts an odd edge case with pivots and vertical layout groups
            // @HACK if the height of context list items changes, this will also need to be adjusted
            position.y -= 24f;

            //resize the width fo the context menu
            if(t is RectTransform rt)
            {
                float width = rt.sizeDelta.x;
                RectTransform contextRect = contextMenu.transform as RectTransform;
                Vector2 size = contextRect.sizeDelta;
                size.x = width;
                contextRect.sizeDelta = size;
            }

            ListItem.HideListItems<ContextMenuListItem>();
            contextMenuPreviousSelection = previousSelection;
            contextMenu.SetActive(true);
            contextMenu.transform.position = position;
            bool selectionMade = false;

            Selectable lastSelection = null;
            Selectable optionToSelect = null;

            foreach(var option in options)
            {
                ListItem li = ListItem.GetListItem<ContextMenuListItem>(contextMenuListItemPrefab, contextMenuList, colorScheme);
                li.Setup(option.name, option.action);
                li.SetColorScheme(colorScheme);

                // Setup custom navigation
                {
                    Navigation nav = li.selectable.navigation;
                    nav.mode = Navigation.Mode.Explicit;
                    nav.selectOnLeft = null;
                    nav.selectOnRight = null;
                    nav.selectOnUp = lastSelection;
                    nav.selectOnDown = null;
                    li.selectable.navigation = nav;
                }

                // if last selection != null, make this list item the 'down' selection for the previous
                if(lastSelection != null)
                {
                    Navigation nav = lastSelection.navigation;
                    nav.selectOnDown = li.selectable;
                    lastSelection.navigation = nav;
                }

                lastSelection = li.selectable;

                // if this is the first context option, make it selected
                if(!selectionMade)
                {
                    optionToSelect = li.selectable;
                    selectionMade = true;
                }
            }

            if (!mouseAndKeyboardNavigation)
            {
                optionToSelect?.Select();
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(contextMenuList as RectTransform);
        }

        /// <summary>
        /// hides the context menu and attempts to move the selection to whatever it was prior to
        /// opening the context menu
        /// </summary>
        public void CloseContextMenu()
        {
            contextMenu.SetActive(false);
            if(contextMenuPreviousSelection != null)
            {
                contextMenuPreviousSelection.Select();
            }
        }

#endregion

#region ModManagement Operation
        /*
         * This region contains the methods used for handling the ModManagementEventDelegate
         * as well as the ProgressHandle given from GetCurrentModManagementOperation()
         */
        
        /// <summary>
        /// This is assigned when the browser is initialised and EnableModManagement is invoked
        /// </summary>
        /// <param name="type">the type of MM event</param>
        /// <param name="id">the mod id pertaining to this event</param>
        internal void ModManagementEvent(ModManagementEventType type, ModId id)
        {
            currentModManagementOperationHandle = ModIOUnity.GetCurrentModManagementOperation();
            if(currentModManagementOperationHandle.Completed)
            {
                currentModManagementOperationHandle = null;
            }

            CacheLocalSubscribedModStatuses();

            if(CollectionModListItem.listItems.ContainsKey(id))
            {
                CollectionModListItem.listItems[id].UpdateStatus(type);
            }
            if(BrowserModListItem.listItems.ContainsKey(id))
            {
                BrowserModListItem.listItems[id].UpdateStatus(type, id);
            }
            if(featuredProfiles != null)
            {
                foreach(var mod in featuredSlotListItems)
                {
                    if(featuredProfiles[mod.profileIndex].id == id)
                    {
                        mod.progressTab.UpdateStatus(type, id);
                    }
                }
            }
            if(DownloadQueuePanel.activeSelf)
            {
                RefreshDownloadHistoryPanel();
            }
        }

        /// <summary>
        /// This looks for progress bars across the UI and invokes an update if needed, using the
        /// provided ProgressHandle.Progress field to do so.
        /// </summary>
        /// <param name="handle">this can be null and will be handled appropriately</param>
        internal void UpdateProgressState(ProgressHandle handle)
        {
            if(handle == null)
            {
                currentModManagementOperationHandle = ModIOUnity.GetCurrentModManagementOperation();
            }

            UpdateAvatarDownloadProgressBar(handle);

            if(Instance.CollectionPanel.activeSelf)
            {
                if(handle != null && CollectionModListItem.listItems.ContainsKey(handle.modId))
                {
                    CollectionModListItem.listItems[handle.modId].UpdateProgressState(handle);
                }
            }
            else if(ModDetailsPanel.activeSelf)
            {
                UpdateModDetailsDownloadProgress(handle);
            }
            else if(handle != null && Instance.BrowserPanel.activeSelf)
            {
                if(BrowserModListItem.listItems.ContainsKey(handle.modId))
                {
                    BrowserModListItem.listItems[handle.modId].UpdateProgressBar(handle);
                }

                if(featuredProfiles != null)
                {
                    foreach(var mod in featuredSlotListItems)
                    {
                        mod.progressTab.UpdateProgress(handle);
                    }
                }
            }
            else if(handle != null && Instance.SearchResultsPanel.activeSelf)
            {
                if(SearchResultListItem.listItems.ContainsKey(handle.modId))
                {
                    SearchResultListItem.listItems[handle.modId].UpdateProgressBar(handle);
                }

                if(featuredProfiles != null)
                {
                    foreach(var mod in featuredSlotListItems)
                    {
                        mod.progressTab.UpdateProgress(handle);
                    }
                }
            }

            // Check this separately because it's an overlay
            if(DownloadQueuePanel.activeSelf)
            {
                UpdateDownloadQueueCurrentDownloadDisplay(handle);
            }
        }

        // Runs at the end of every frame so long as the browser is active, and gets the most recent
        // mod management operation to be run via the UpdateProgressState() method
        void LateUpdate()
        {
            if(BrowserCanvas.activeSelf)
            {
                UpdateProgressState(currentModManagementOperationHandle);
            }
        }
#endregion
    }
}
