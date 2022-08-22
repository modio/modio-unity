using System.Collections;
using ModIO;
using ModIOBrowser.Implementation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModIOBrowser
{
    /// <summary>
    /// This partial class contains all of the methods and behaviour concerning the DownloadQueue
    /// panel.
    /// </summary>
    public partial class Browser
    {
        [Header("Download History Panel")]
        [SerializeField] GameObject DownloadQueuePanel;
        [SerializeField] GameObject DownloadQueueCurrentProgressBar;
        [SerializeField] TMP_Text DownloadQueueCurrentJobText;
        [SerializeField] Image DownloadQueueCurrentProgressBarFill;
        [SerializeField] TMP_Text DownloadQueueUsernameText;
        [SerializeField] TMP_Text DownloadQueueCurrentModName;
        [SerializeField] TMP_Text DownloadQueueCurrentDownloadedAmount;
        [SerializeField] TMP_Text DownloadQueueCurrentDownloadSpeed;
        [SerializeField] Button DownloadQueueCurrentUnsubscribeButton;
        [SerializeField] Button DownloadQueueCurrentLogoutButton;
        [SerializeField] Transform DownloadQueueList;
        [SerializeField] RectTransform DownloadQueueListViewport;
        [SerializeField] GameObject DownloadQueueListItem;
        [SerializeField] GameObject DownloadQueueNoPendingNotice;
        [SerializeField] GameObject DownloadQueueNoCurrentNotice;
        [SerializeField] Image Avatar_DownloadQueue;
        ModProfile downloadQueueCurrentModProfileOfOperationInProgress;

        // This is set when the panel is opened and selected when closed
        Selectable downloadQueueSelectionOnClose;

#region Mod Download History Panel

        /// <summary>
        /// Use this to toggle the menu on or off. Used by the OnMenu input press.
        /// TODO @Steve for consistency consider removing this method as it's not necessary
        /// </summary>
        internal void ToggleDownloadQueuePanel()
        {
            if(DownloadQueuePanel.activeSelf)
            {
                CloseDownloadQueuePanel();
            }
            else
            {                
                // TODO this selection can sometimes fail, figure out why it would be null (not dire as we have a backup)
                OpenDownloadQueuePanel(EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>());
            }
        }

        internal void OpenDownloadQueuePanel(Selectable selectionOnClose = null)
        {
            downloadQueueSelectionOnClose = selectionOnClose ?? downloadQueueSelectionOnClose;
            DownloadQueuePanel.SetActive(true);
            RefreshDownloadHistoryPanel();
            SelectionManager.Instance.SelectView(UiViews.Downloads);
        }
        
        public void CloseDownloadQueuePanel()
        {            
            DownloadQueuePanel.SetActive(false);
            SelectSelectable(downloadQueueSelectionOnClose);
            RefreshDownloadHistoryPanel();
            SelectionManager.Instance.SelectPreviousView();
        }
        
        /// <summary>
        /// This will find mods that are subscribed but not installed and add a row for them in the
        /// download queue list. It will also update the header/current mod being downloaded.
        /// </summary>
        internal void RefreshDownloadHistoryPanel()
        {
            DownloadQueueNoPendingNotice.SetActive(false);
            DownloadQueueNoCurrentNotice.SetActive(false);
            
            // Username
            DownloadQueueUsernameText.text = currentUserProfile.portal_username 
                                             ?? currentUserProfile.username;
            
            // Setup explicit navigation for current Unsubscibe button
            Navigation mainUnsubscribeNavigation = DownloadQueueCurrentUnsubscribeButton.navigation;
            mainUnsubscribeNavigation.mode = Navigation.Mode.Explicit;
            mainUnsubscribeNavigation.selectOnUp = DownloadQueueCurrentLogoutButton;
            mainUnsubscribeNavigation.selectOnLeft = null;
            mainUnsubscribeNavigation.selectOnRight = null;
            
            bool pendingModsInQueue = false;

            ListItem.HideListItems<DownloadQueueListItem>();

            Selectable lastListItemSelectable = null;
            
            foreach(SubscribedMod mod in subscribedMods)
            {
                if(mod.status == SubscribedModStatus.Installed || pendingUnsubscribes.Contains(mod.modProfile.id))
                {
                    continue;
                }
                if(currentModManagementOperationHandle?.modId == mod.modProfile.id)
                {
                    continue;
                }
                
                ListItem li = ListItem.GetListItem<DownloadQueueListItem>(DownloadQueueListItem, DownloadQueueList, colorScheme);
                li.Setup(mod);
                li.SetViewportRestraint(DownloadQueueList as RectTransform, DownloadQueueListViewport);
                pendingModsInQueue = true;
                
                // Setup explicit navigation
                Navigation navigation = li.selectable.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = null;
                navigation.selectOnDown = null;
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
                
                // First item in list
                if(lastListItemSelectable == null)
                {
                    navigation.selectOnUp = DownloadQueueCurrentUnsubscribeButton;
                    mainUnsubscribeNavigation.selectOnDown = li.selectable;
                }
                // every other list item
                else
                {
                    Navigation lastNavigation = lastListItemSelectable.navigation;
                    navigation.selectOnUp = lastListItemSelectable;
                    lastNavigation.selectOnDown = li.selectable;
                    lastListItemSelectable.navigation = lastNavigation;
                }

                li.selectable.navigation = navigation;
                lastListItemSelectable = li.selectable;
            }

            // Set navigation settings now that list items have populated
            DownloadQueueCurrentUnsubscribeButton.navigation = mainUnsubscribeNavigation;
            
            // If no mods are pending, show notice
            if(!pendingModsInQueue)
            {
                DownloadQueueNoPendingNotice.SetActive(true);
            }
            
            // Check the selection isn't lost on a de-activated button
            if (!DownloadQueueCurrentUnsubscribeButton.gameObject.activeSelf)
            {
                SelectSelectable(DownloadQueueCurrentLogoutButton);
            }
        }

        void UpdateDownloadQueueCurrentDownloadDisplay(ProgressHandle handle)
        {
            DownloadQueueNoCurrentNotice.SetActive(false);

            bool noModFound = true;
            if(downloadQueueCurrentModProfileOfOperationInProgress.id != handle?.modId)
            {
                foreach(SubscribedMod mod in subscribedMods)
                {
                    if(mod.modProfile.id == handle?.modId)
                    {
                        noModFound = false;
                        downloadQueueCurrentModProfileOfOperationInProgress = mod.modProfile;
                        break;
                    }
                }
            }
            else
            {
                noModFound = false;
            }

            if(noModFound)
            {
                DownloadQueueCurrentUnsubscribeButton.gameObject.SetActive(false);
                DownloadQueueCurrentProgressBar.SetActive(false);
                DownloadQueueNoCurrentNotice.SetActive(true);
                return;
            }

            DownloadQueueCurrentJobText.text = handle.OperationType == ModManagementOperationType.Download ? "Downloading" : "Installing";
            DownloadQueueCurrentModName.text = downloadQueueCurrentModProfileOfOperationInProgress.name;
            DownloadQueueCurrentDownloadSpeed.text = handle.OperationType == ModManagementOperationType.Download ? Utility.GenerateHumanReadableStringForBytes(handle.BytesPerSecond) : "";
            DownloadQueueCurrentDownloadedAmount.text = ""; // TODO filesize * progress
            DownloadQueueCurrentProgressBarFill.fillAmount = handle.Progress;
            DownloadQueueCurrentProgressBar.SetActive(true);
            DownloadQueueCurrentUnsubscribeButton.gameObject.SetActive(true);
        }

        public void UnsubscribeToCurrentDownloadQueueOperation()
        {
            UnsubscribeFromModEvent(downloadQueueCurrentModProfileOfOperationInProgress);
            RefreshDownloadHistoryPanel();
        }
        
        // TODO @Steve clearing user data has been failing for some reason, investigate
        /// <summary>
        /// This is the method attached to the logout button in the DownloadQueue panel
        /// </summary>
        public void LogoutButton()
        {
            ToggleDownloadQueuePanel();
            OpenAuthenticationPanel_Logout(delegate { OpenDownloadQueuePanel(); });
        }

#endregion // Mod Download History Panel

    }
}
