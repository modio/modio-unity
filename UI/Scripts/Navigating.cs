using ModIOBrowser.Implementation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIOBrowser
{
    //Calling this class navigation would be preferable, alas, Navigation is a keyword in Unity

    static class Navigating
    {
        internal static void Cancel()
        {
            if(ModioContextMenu.Instance.ContextMenu.activeSelf)
            {
                ModioContextMenu.Instance.Close();
            }
            else if(MultiTargetDropdown.currentMultiTargetDropdown != null)
            {
                MultiTargetDropdown.currentMultiTargetDropdown.Hide();
                MultiTargetDropdown.currentMultiTargetDropdown = null;
            }
            else if(SearchPanel.Instance.SearchPanelGameObject.activeSelf)
            {
                SearchPanel.Instance.Close();
            }
            else if(AuthenticationPanels.Instance.AuthenticationPanel.activeSelf)
            {
                Authentication.Instance.Close();
            }
            else if(DownloadQueue.Instance.DownloadQueuePanel.activeSelf)
            {
                DownloadQueue.Instance.ToggleDownloadQueuePanel();
            }
            else if(Details.Instance.ModDetailsPanel.activeSelf)
            {
                Details.Instance.Close();
            }
            else if(Collection.Instance.uninstallConfirmationPanel.activeSelf)
            {
                Collection.Instance.CloseUninstallConfirmation();
            }
            else if(Browser.currentFocusedPanel != Home.Instance.BrowserPanel)
            {
                Home.Instance.Open();
            }
            else
            {
                Browser.Close();
            }
        }

        /// <summary>
		/// Used as a secondary 'submit' option.
		/// </summary>
        internal static void Alternate()
        {
            if(SearchPanel.Instance.SearchPanelGameObject.activeSelf)
            {
                // Apply filters and show results
                SearchPanel.Instance.ApplyFilter();
            }
            else if(Home.IsOn())
            {
                // if overlay browser inflated, subscribe/unsubscribe
                // if featured item highlighted/selected subscribe/unsubscribe
                if(!SelectionOverlayHandler.TryAlternateForBrowserOverlayObject() && Home.Instance.isFeaturedItemSelected)
                {
                    Home.Instance.SubscribeToFeaturedMod();
                }
            }
            else if(Details.Instance.ModDetailsPanel.activeSelf)
            {
                // if mod details panel selected subscribe/unsubscribe
                Details.Instance.SubscribeButtonPress();
            }
            else if(Collection.IsOn())
            {
                if(Collection.Instance.currentSelectedCollectionListItem != null)
                {
                    Collection.Instance.currentSelectedCollectionListItem.UnsubscribeButton();
                }
            }
            else if(SearchResults.Instance.SearchResultsPanel.activeSelf)
            {
                // if search results list item selected subscribe/unsubscribe}
                SelectionOverlayHandler.TryAlternateForSearchResultsOverlayObject();
            }
        }

        internal static void Options()
        {
            // if browser overlay inflated, bring up more options
            // if browser featured selected bring up more options
            if(ModioContextMenu.Instance.ContextMenu.activeSelf)
            {
                ModioContextMenu.Instance.Close();
            }
            else if(SearchResults.Instance.SearchResultsPanel.activeSelf)
            {
                // if search results list item selected subscribe/unsubscribe}
                SelectionOverlayHandler.TryToOpenMoreOptionsForSearchResultsOverlayObject();
            }
            else if(Collection.IsOn())
            {
                if(Collection.Instance.currentSelectedCollectionListItem != null)
                {
                    Collection.Instance.currentSelectedCollectionListItem.ShowMoreOptions();
                }
            }
            else if(SearchPanel.Instance.SearchPanelGameObject.activeSelf)
            {
                // clear filter results
                SearchPanel.searchFilterTags.Clear();
                SearchPanel.Instance.SearchPanelField.text = "";
                SearchPanel.Instance.SetupTags();
            }
            else if(Home.IsOn())
            {
                // if overlay browser inflated, subscribe/unsubscribe
                // if featured item highlighted/selected subscribe/unsubscribe
                if(!SelectionOverlayHandler.TryToOpenMoreOptionsForBrowserOverlayObject() && Home.Instance.isFeaturedItemSelected)
                {
                    Home.Instance.OpenMoreOptionsForFeaturedSlot();
                }
            }
        }

        internal static void TabLeft()
        {
            if(SearchPanel.Instance.SearchPanelGameObject.activeSelf)
            {
                TagJumpToSelection.GoToPreviousSelection();
            }
            else if(Details.Instance.ModDetailsPanel.activeSelf)
            {
                Details.Instance.GalleryImageTransition(false);
            }
            else if(Home.IsOn() || Collection.IsOn())
            {
                ToggleBetweenBrowserAndCollection();
            }
        }

        internal static void TabRight()
        {
            if(SearchPanel.Instance.SearchPanelGameObject.activeSelf)
            {
                TagJumpToSelection.GoToNextSelection();
            }
            else if(Details.Instance.ModDetailsPanel.activeSelf)
            {
                Details.Instance.GalleryImageTransition(true);
            }
            else if(Home.IsOn() || Collection.IsOn())
            {
                ToggleBetweenBrowserAndCollection();
            }
        }

        internal static void MenuInput()
        {
            OpenMenuProfile();
        }

        internal static void Scroll(float direction)
        {
            if(Details.Instance.ModDetailsPanel.activeSelf
               && !Reporting.Instance.Panel.activeSelf
               && EventSystem.current.currentSelectedGameObject == Details.Instance.ModDetailsScrollToggleGameObject)
            {
                Vector3 position = Details.Instance.ModDetailsContentRect.position;
                position.y += direction * (100f * Time.fixedDeltaTime) * -1f;
                Details.Instance.ModDetailsContentRect.position = position;
            }
        }


        /// <summary>
        /// Closes all panels and opens the specified one.
        /// </summary>
        /// <param name="panel">the new GameObject UI panel to enable</param>
        internal static void GoToPanel(GameObject panel)
        {
            // Ensure no other panels are open
            CloseAll();

            // Open the specified panel
            panel?.SetActive(true);

            Browser.currentFocusedPanel = panel;
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
            if(Home.IsOn())
            {
                Home.Instance.BrowserPanel.SetActive(false);
            }
            if(Collection.IsOn())
            {
                Collection.Instance.CollectionPanel.SetActive(false);
            }
            if(Details.IsOn())
            {
                Details.Instance.ModDetailsPanel.SetActive(false);
            }
            if(SearchPanel.Instance.SearchPanelGameObject.activeSelf)
            {
                SearchPanel.Instance.SearchPanelGameObject.SetActive(false);
            }
            if(SearchResults.Instance.SearchResultsPanel.activeSelf)
            {
                SearchResults.Instance.SearchResultsPanel.SetActive(false);
                SelectionOverlayHandler.Instance.SearchResultListItemOverlay.gameObject.SetActive(false);
            }
            if(AuthenticationPanels.Instance.AuthenticationPanel.activeSelf)
            {
                AuthenticationPanels.Instance.AuthenticationPanel.SetActive(false);
            }
            if(DownloadQueue.Instance.DownloadQueuePanel.activeSelf)
            {
                DownloadQueue.Instance.DownloadQueuePanel.SetActive(false);
            }
            if(ModioContextMenu.Instance.ContextMenu.activeSelf)
            {
                ModioContextMenu.Instance.ContextMenu.SetActive(false);
            }
            if(Reporting.Instance.Panel.activeSelf)
            {
                Reporting.Instance.Panel.SetActive(false);
            }
        }

        /// <summary>
        /// Depending on the authentication status this will either open the download history panel
        /// or the authentication wizard dialog.
        /// </summary>
        public static void OpenMenuProfile()
        {
            if(!Authentication.Instance.IsAuthenticated)
            {
                // Toggle authentication panel
                if(AuthenticationPanels.Instance.AuthenticationPanel.activeSelf)
                {
                    Authentication.Instance.Close();
                }
                else
                {
                    AuthenticationPanels.Instance.Open();
                }
            }
            else
            {
                DownloadQueue.Instance.ToggleDownloadQueuePanel();
            }
        }

        /// <summary>
        /// This method is used when a left or right bumper is pressed and the view needs to change
        /// from the home view to the collection view and vice versa.
        /// </summary>
        public static void ToggleBetweenBrowserAndCollection()
        {
            if(Home.IsOn())
            {
                Collection.Instance.Open();
            }
            else
            {
                Home.Instance.Open();
            }
        }

    }
}
