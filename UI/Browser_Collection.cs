using System;
using System.Collections;
using System.Collections.Generic;
using ModIO;
using ModIOBrowser.Implementation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    /// <summary>
    /// This partial class contains all of the methods and behaviour concerning the Collection panel
    /// </summary>
    public partial class Browser
    {
        [Header("Collection Panel")]
        [SerializeField] GameObject CollectionPanel;
        [SerializeField] TMP_Text CollectionPanelTitle;
        [SerializeField] InputField CollectionPanelSearchField;
        [SerializeField] GameObject CollectionPanelModListItem;
        [SerializeField] RectTransform CollectionPanelContentParent;
        [SerializeField] Scrollbar CollectionPanelContentScrollBar;
        [SerializeField] Transform CollectionPanelModListItemParent;
        [SerializeField] TMP_Text CollectionPanelCheckForUpdatesText;
        [SerializeField] Button CollectionPanelCheckForUpdatesButton;
        [SerializeField] MultiTargetDropdown CollectionPanelFirstDropDownFilter;
        [SerializeField] MultiTargetDropdown CollectionPanelSecondDropDownFilter;
        [SerializeField] TMP_Text CollectionPanelNavButton;
        [SerializeField] GameObject CollectionPanelNavButtonHighlights;
        [SerializeField] Image CollectionPanelHeaderBackground;
        internal CollectionModListItem currentSelectedCollectionListItem;
        SubscribedMod[] subscribedMods;
        InstalledMod[] installedMods;
        Dictionary<ModId, string> modStatus = new Dictionary<ModId, string>();
        bool checkingForUpdates = false;
        List<ModProfile> pendingSubscriptions = new List<ModProfile>();
        HashSet<ModId> pendingUnsubscribes = new HashSet<ModId>();
        IEnumerator collectionHeaderTransition;
        float collectionHeaderLastAlphaTarget = -1;
        
        [Header("Uninstall Confirmation")]
        [SerializeField] GameObject uninstallConfirmationPanel;
        [SerializeField] TMP_Text uninstallConfirmationPanelModName;
        [SerializeField] TMP_Text uninstallConfirmationPanelFileSize;
        [SerializeField] Selectable uninstallConfirmationPanelDefaultSelection;
        ModProfile currentSelectedModForUninstall;

#region Mod Collection
        public void OpenModCollection()
        {
            GoToPanel(CollectionPanel);
            RefreshCollectionListItems();
            UpdateNavbarSelection();
        }

        internal void RefreshLocalModCollection()
        {
            CacheLocalSubscribedModStatuses();

            modStatus.Clear();
            foreach(SubscribedMod mod in subscribedMods)
            {
                modStatus.Add(mod.modProfile.id, Utility.GetModStatusAsString(mod));
            }
            foreach(InstalledMod mod in installedMods)
            {
                modStatus.Add(mod.modProfile.id, "Installed");
            }
            foreach(ModProfile mod in pendingSubscriptions)
            {
                modStatus.Add(mod.id, "Pending...");
            }
        }

        public void RefreshCollectionListItems(bool forceSelection = true)
        {
            // TODO refresh existing list items so we dont lose/change selection (feels jarring) and
            // TODO cull no longer required list items
            // TODO only hide/enable items we need to, dont do a complete refresh
            
            CollectionPanelCheckForUpdatesText.text = checkingForUpdates ? "Checking..." : "Check for updates";
            RefreshLocalModCollection();
            
            //--------------------------------------------------------------------------------//
            //                              GET MODS TO DISPLAY                               //
            //--------------------------------------------------------------------------------//
            List<ModProfile> subscribedAndPending = new List<ModProfile>();

            List<SubscribedMod> subscribed = new List<SubscribedMod>(subscribedMods);
            foreach(SubscribedMod mod in subscribed)
            {
                subscribedAndPending.Add(mod.modProfile);
            }
            subscribedAndPending.AddRange(pendingSubscriptions);
            List<InstalledMod> installed = new List<InstalledMod>(installedMods);
            
            CollectionPanelTitle.text = subscribedMods == null ? "Collection" : $"Collection <size=30>({subscribedAndPending.Count})</size>";
            
            //--------------------------------------------------------------------------------//
            //                              GET FILTER SETTINGS                               //
            //--------------------------------------------------------------------------------//
            // check the first dropdown filter to decide if we show/hide subs/unsubs
            bool hideSubs = false;
            switch(CollectionPanelFirstDropDownFilter.value)
            {
                case 0:
                    hideSubs = false;
                    break;
                case 1:
                    hideSubs = true;
                    break;
                case 2:
                    hideSubs = false;
                    break;
            }
            
            // Sort the lists of mods according to dropdown filters
            switch(CollectionPanelSecondDropDownFilter.value)
            {
                case 0:
                    subscribedAndPending.Sort(Utility.CompareModProfilesAlphabetically);
                    installed.Sort(Utility.CompareModProfilesAlphabetically);
                    break;
                case 1:
                    subscribedAndPending.Sort(Utility.CompareModProfilesByFileSize);
                    installed.Sort(Utility.CompareModProfilesByFileSize);
                    break;
            }

            // Check if we have a search phrase to compare against mods
            HashSet<ModId> omittedBySearchPhrase = new HashSet<ModId>();

            if(CollectionPanelSearchField.text.Length > 0)
            {
                string searchPhrase = CollectionPanelSearchField.text;
                
                foreach(ModProfile modProfile in subscribedAndPending)
                {
                    if(modProfile.name.IndexOf(searchPhrase, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        omittedBySearchPhrase.Add(modProfile.id);
                    }
                }
                
                foreach(InstalledMod mod in installed)
                {
                    if(mod.modProfile.name.IndexOf(searchPhrase, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        omittedBySearchPhrase.Add(mod.modProfile.id);
                    }
                }
            }


            //--------------------------------------------------------------------------------//
            //                                 DISPLAY MODS                                   //
            //--------------------------------------------------------------------------------//
            // hide list items @UNDONE not needed with current setup
            // ListItem.HideListItems<CollectionModListItem>();

            bool hasSelection = !forceSelection;

            // SUBSCRIBED MODS
            foreach(ModProfile mod in subscribedAndPending)
            {
                bool hide = hideSubs ? hideSubs : omittedBySearchPhrase.Contains(mod.id);
                
                // first check if we have an active list item for this
                if(CollectionModListItem.listItems.ContainsKey(mod.id))
                {
                    if(pendingUnsubscribes.Contains(mod.id) || hide)
                    {
                        CollectionModListItem.listItems[mod.id].gameObject.SetActive(false);
                        continue;
                    }
                    CollectionModListItem.listItems[mod.id].Setup(mod, true, modStatus[mod.id]);
                    CollectionModListItem.listItems[mod.id].SetViewportRestraint(CollectionPanelContentParent, null);
                    if(!hasSelection)
                    {
                        hasSelection = true;
                        CollectionModListItem.listItems[mod.id].selectable?.Select();
                        SetExplicitDownNavigationForTopRowButtonsInCollectionPanel(CollectionModListItem.listItems[mod.id].selectable);
                    }
                } 
                // make a new list item
                else
                {
                    if(pendingUnsubscribes.Contains(mod.id) || hide)
                    {
                        continue;
                    }
                    ListItem li = ListItem.GetListItem<CollectionModListItem>(CollectionPanelModListItem, CollectionPanelModListItemParent, colorScheme);
                    li.Setup(mod, true, modStatus[mod.id]);
                    li.SetViewportRestraint(CollectionPanelContentParent, null);
                    if(!hasSelection)
                    {
                        hasSelection = true;
                        li.selectable?.Select();
                        SetExplicitDownNavigationForTopRowButtonsInCollectionPanel(li.selectable);
                    }
                }
            }
            
            // INSTALLED MODS
            if(installed != null && installed.Count > 0)
            {
                foreach(InstalledMod mod in installed)
                { 
                    ModId modId = mod.modProfile.id;
                    
                    // Check if this has a pending subscription, if so, hide it
                    bool hide = hideSubs ? hideSubs : mod.subscribedUsers.Count > 0;

                    // check if we need to hide this if it's filtered out via search phrase
                    if(!hide)
                    {
                        hide = omittedBySearchPhrase.Contains(modId);
                    }
                    
                    // if we dont need to hide this, check if the current user has a pending
                    // subscription, in which case we'll create a list item from iterating over
                    // pendingSubscriptions instead of here
                    if (!hide)
                    {
                        foreach(ModProfile profile in pendingSubscriptions)
                        {
                            if(profile.id.Equals(modId))
                            {
                                hide = true;
                                break;
                            }
                        }
                    }
                    
                    // first check if we have an active list item for this
                    if(CollectionModListItem.listItems.ContainsKey(modId))
                    {
                        if(hide)
                        {
                            CollectionModListItem.listItems[modId].gameObject.SetActive(false);
                            continue;
                        }
                        CollectionModListItem.listItems[modId].Setup(mod);
                        if(!hasSelection)
                        {
                            hasSelection = true;
                            CollectionModListItem.listItems[modId].selectable?.Select();
                            SetExplicitDownNavigationForTopRowButtonsInCollectionPanel(CollectionModListItem.listItems[modId].selectable);
                        }
                    }
                    // make a new list item
                    else
                    {
                        if(hide)
                        {
                            continue;
                        }
                        ListItem li = ListItem.GetListItem<CollectionModListItem>(CollectionPanelModListItem, CollectionPanelModListItemParent, colorScheme);
                        li.Setup(mod);
                        li.SetViewportRestraint(CollectionPanelContentParent, null);
                        if(!hasSelection)
                        {
                            hasSelection = true;
                            li.selectable?.Select();
                            SetExplicitDownNavigationForTopRowButtonsInCollectionPanel(li.selectable);
                        }
                    }
                }
            }
            
            if(!hasSelection)
            {
                defaultCollectionSelection?.Select();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(CollectionPanelModListItemParent as RectTransform);
        }

        /// <summary>
        /// This is to circumvent an odd behaviour when pressing 'down' to move the selection was
        /// jumping to the third, fourth or fifth list item in the collection panel list.
        /// </summary>
        /// <param name="selectable">This should be the first item in the collection list</param>
        void SetExplicitDownNavigationForTopRowButtonsInCollectionPanel(Selectable selectable)
        {
            // updates button
            Navigation updatesButton = CollectionPanelCheckForUpdatesButton.navigation;
            updatesButton.selectOnDown = selectable;
            CollectionPanelCheckForUpdatesButton.navigation = updatesButton;
            
            // first dropdown
            Navigation firstDropdown = CollectionPanelFirstDropDownFilter.navigation;
            firstDropdown.selectOnDown = selectable;
            CollectionPanelFirstDropDownFilter.navigation = firstDropdown;
            
            // second dropdown
            Navigation secondDropdown = CollectionPanelSecondDropDownFilter.navigation;
            secondDropdown.selectOnDown = selectable;
            CollectionPanelSecondDropDownFilter.navigation = secondDropdown;
        }
        
        public void CheckForUpdates()
        {
            if(checkingForUpdates)
            {
                return;
            }
            CollectionPanelCheckForUpdatesText.text = "Checking...";
            ModIOUnity.FetchUpdates(FinishedCheckingForUpdates);
            checkingForUpdates = true;
        }
        
        public void CollectionPanelOnScrollValueChange()
        {
            float targetAlpha = -1f;
            
            // Get the target alpha based on what the scrollbar value is
            if(CollectionPanelContentScrollBar.value < 1f)
            {
                targetAlpha = CollectionPanelHeaderBackground.color.a == 1f ? targetAlpha : 1f;
            }
            else
            {
                targetAlpha = CollectionPanelHeaderBackground.color.a == 0f ? targetAlpha : 0f;
            }

            // If the target alpha needs to change, start the transition coroutine here
            if(targetAlpha != -1f && targetAlpha != collectionHeaderLastAlphaTarget)
            {
                collectionHeaderLastAlphaTarget = targetAlpha;
                if(collectionHeaderTransition != null)
                {
                    StopCoroutine(collectionHeaderTransition);
                }
                collectionHeaderTransition = TransitionImageAlpha(CollectionPanelHeaderBackground, targetAlpha);
                StartCoroutine(collectionHeaderTransition);
            }
        }

        void FinishedCheckingForUpdates(Result result)
        {
            checkingForUpdates = false;
            if(result.Succeeded())
            {
                RefreshCollectionListItems();
            }
            CollectionPanelCheckForUpdatesText.text = "Check for updates";
        }

#endregion // Mod Collection

#region Confirm Unsubscibe / Uninstall
        public void CloseUninstallConfirmation()
        {
            uninstallConfirmationPanel.SetActive(false);
        }

        public void OpenUninstallConfirmation(ModProfile profile)
        {
            uninstallConfirmationPanelModName.text = profile.name;
            uninstallConfirmationPanelFileSize.text = ""; // TODO need to add file size
            currentSelectedModForUninstall = profile;
            uninstallConfirmationPanel.SetActive(true);
            uninstallConfirmationPanelDefaultSelection?.Select();
        }

        public void ConfirmUninstall()
        {
            CloseUninstallConfirmation();
            if (IsSubscribed(currentSelectedModForUninstall.id))
            {
                UnsubscribeFromModEvent(currentSelectedModForUninstall);
            }
            else
            {
                ModIOUnity.ForceUninstallMod(currentSelectedModForUninstall.id);
            }
            RefreshCollectionListItems();
        }
        
#endregion

    }
}
