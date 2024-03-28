using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ModIO;
using ModIO.Util;

namespace ModIOBrowser.Implementation
{
    public class Collection : SelfInstancingMonoSingleton<Collection>
    {
        [Header("Collection Panel")]
        [SerializeField]
        public GameObject CollectionPanel;
        [SerializeField] TMP_Text CollectionPanelTitle;
        [SerializeField] TMP_InputField CollectionPanelSearchField;
        [SerializeField] GameObject CollectionPanelModListItem;
        [SerializeField] RectTransform CollectionPanelContentParent;
        [SerializeField] Scrollbar CollectionPanelContentScrollBar;
        [SerializeField] Transform CollectionPanelModListItemParent;
        [SerializeField] TMP_Text CollectionPanelCheckForUpdatesText;
        [SerializeField] Button CollectionPanelCheckForUpdatesButton;
        [SerializeField] MultiTargetDropdown CollectionPanelFirstDropDownFilter;
        [SerializeField] MultiTargetDropdown CollectionPanelSecondDropDownFilter;
        [SerializeField] Image CollectionPanelHeaderBackground;
        [SerializeField] Selectable defaultCollectionSelection;
        internal CollectionModListItem currentSelectedCollectionListItem;
        public ModProfile[] purchasedMods = Array.Empty<ModProfile>();
        public SubscribedMod[] subscribedMods = Array.Empty<SubscribedMod>();
        public InstalledMod[] installedMods = Array.Empty<InstalledMod>();

        public List<ModProfile> pendingSubscriptions = new List<ModProfile>();
        public HashSet<ModId> pendingUnsubscribes = new HashSet<ModId>();
        public HashSet<ModId> notEnoughSpaceForTheseMods = new HashSet<ModId>();

        private Dictionary<ModId, string> modStatus = new Dictionary<ModId, string>();
        private bool checkingForUpdates = false;
        private IEnumerator collectionHeaderTransition;
        private float collectionHeaderLastAlphaTarget = -1;

        [Header("Uninstall Confirmation")]
        [SerializeField]
        public GameObject uninstallConfirmationPanel;
        [SerializeField] TMP_Text uninstallConfirmationPanelModName;
        [SerializeField] TMP_Text uninstallConfirmationPanelFileSize;
        ModProfile currentSelectedModForUninstall;

        internal Translation CollectionPanelCheckForUpdatesTextTranslation = null;
        internal Translation CollectionPanelTitleTranslation = null;

        public static bool IsOn() => Instance != null && Instance.CollectionPanel != null && Instance.CollectionPanel.activeSelf;

        // TODO This region is for the Uninstall confirmation dialog, and needs to be merged to the reusable dialog instead
#region Confirm Unsubscibe / Uninstall
        public void CloseUninstallConfirmation()
        {
            uninstallConfirmationPanel.SetActive(false);
            SelectionManager.Instance.SelectView(UiViews.Collection);
        }

        public void OpenUninstallConfirmation(ModProfile profile)
        {
            uninstallConfirmationPanelModName.text = profile.name;
            uninstallConfirmationPanelFileSize.text = ""; // TODO need to add file size
            currentSelectedModForUninstall = profile;
            uninstallConfirmationPanel.SetActive(true);
            SelectionManager.Instance.SelectView(UiViews.ConfirmUninstall);
        }

        public void ConfirmUninstall()
        {
            CloseUninstallConfirmation();
            if (Collection.Instance.IsSubscribed(currentSelectedModForUninstall.id))
            {
                Mods.UnsubscribeFromEvent(currentSelectedModForUninstall);
            }
            else
            {
                ModIOUnity.ForceUninstallMod(currentSelectedModForUninstall.id);
            }
            RefreshList();
        }

#endregion

        public void Open()
        {
            Navigating.GoToPanel(CollectionPanel);
            RefreshList();
            NavBar.Instance.UpdateNavbarSelection();
            SelectionManager.Instance.SelectView(UiViews.Collection);
        }

        void Refresh()
        {
            Collection.Instance.CacheLocalSubscribedModStatuses();

            modStatus.Clear();
            foreach(var modProfile in this.purchasedMods)
            {
                modStatus.Add(modProfile.id, "Purchased");
            }
            foreach(SubscribedMod mod in subscribedMods)
            {
                if(!modStatus.ContainsKey(mod.modProfile.id))
                {
                    modStatus.Add(mod.modProfile.id, Utility.GetModStatusAsString(mod));
                }
                else
                {
                    modStatus[mod.modProfile.id] = Utility.GetModStatusAsString(mod);
                }
            }
            foreach(InstalledMod mod in installedMods)
            {
                if (!modStatus.ContainsKey(mod.modProfile.id))
                {
                    modStatus.Add(mod.modProfile.id, "Installed");
                }
                else
                {
                    modStatus[mod.modProfile.id] = "Installed";
                }
            }
            foreach(ModProfile mod in pendingSubscriptions)
            {
                if(!modStatus.ContainsKey(mod.id))
                {
                    modStatus.Add(mod.id, "Pending...");
                }
                else
                {
                    modStatus[mod.id] = "Pending...";
                }
            }
        }

        public void RefreshList()
        {
            // TODO refresh existing list items so we dont lose/change selection (feels jarring) and
            // TODO cull no longer required list items
            // TODO only hide/enable items we need to, dont do a complete refresh
            // TODO dynamically load collection on scroll so we dont try and populate more than a hundred items at a time

            if(checkingForUpdates)
            {
                Translation.Get(CollectionPanelCheckForUpdatesTextTranslation, "Checking...", CollectionPanelCheckForUpdatesText);
            }
            else
            {
                Translation.Get(CollectionPanelCheckForUpdatesTextTranslation, "Check for updates", CollectionPanelCheckForUpdatesText);
            }

            Refresh();

            //--------------------------------------------------------------------------------//
            //                              GET FILTER SETTINGS                               //
            //--------------------------------------------------------------------------------//
            // check the first dropdown filter to decide if we show/hide subs/unsubs
            bool showSubscribed = false;
            bool showUnsubscribed = false;
            bool showPurchased = false;
            switch(CollectionPanelFirstDropDownFilter.value)
            {
                case 0://All Mods
                    showUnsubscribed = true;
                    showSubscribed = true;
                    showPurchased = true;
                    break;
                case 1://Subscribed
                    showSubscribed = true;
                    break;
                case 2://Unsubscribed
                    showUnsubscribed = true;
                    break;
                case 3://Purchased
                    showPurchased = true;
                    break;
            }

            //--------------------------------------------------------------------------------//
            //                              GET MODS TO DISPLAY                               //
            //--------------------------------------------------------------------------------//
            List<CollectionProfile> allMods = new List<CollectionProfile>();

            if (showSubscribed)
            {
                foreach(SubscribedMod mod in subscribedMods)
                {
                    if(pendingUnsubscribes.Contains(mod.modProfile.id))
                    {
                        continue;
                    }
                    allMods.Add(new CollectionProfile(mod.modProfile, true, mod.enabled, 1, modStatus[mod.modProfile.id]));
                }
                foreach(ModProfile mod in pendingSubscriptions)
                {
                    allMods.Add(new CollectionProfile(mod, true, true, 1, modStatus[mod.id]));
                }
            }
            if (showUnsubscribed)
            {
                // cache the pending subs in ModIds for an easier comparison
                List<ModId> pendingSubs = pendingSubscriptions.Select(mod => mod.id).ToList();

                foreach(InstalledMod mod in installedMods)
                {
                    // If we have subscribed to this, dont display it as an 'Unsubscribed' mod
                    if(pendingSubs.Contains(mod.modProfile.id) || mod.subscribedUsers.Count < 1)
                    {
                        continue;
                    }
                    allMods.Add(new CollectionProfile(mod.modProfile, false, false, mod.subscribedUsers.Count, modStatus[mod.modProfile.id]));
                }
            }
            if (showPurchased)
            {
                foreach(ModProfile modProfile in this.purchasedMods)
                {
                    var collectionProfile = new CollectionProfile(modProfile, true, true, 1, modStatus[modProfile.id]);
                    int index = allMods.FindIndex(item => item.modProfile.id == modProfile.id);
                    if(index != -1)
                        allMods[index] = collectionProfile;
                    else
                        allMods.Add(collectionProfile);
                }
            }


            string accentHashColor = ColorUtility.ToHtmlStringRGBA(SharedUi.colorScheme.GetSchemeColor(ColorSetterType.Highlight));

            if(subscribedMods == null)
            {
                Translation.Get(CollectionPanelTitleTranslation, "Collection", CollectionPanelTitle);
            }
            else
            {
                Translation.Get(CollectionPanelTitleTranslation,
                    "Collection <size=20><color=#{accentHashColor}>({subscribedAndPending.Count})</color></size>",
                    CollectionPanelTitle, $"{accentHashColor}", $"{allMods.Count}");
            }


            //--------------------------------------------------------------------------------//
            //                              SORT AND FILTER                                   //
            //--------------------------------------------------------------------------------//

            // Sort the lists of mods according to dropdown filters
            switch(CollectionPanelSecondDropDownFilter.value)
            {
                case 0:
                    allMods.Sort(CompareModProfilesAlphabetically);
                    break;
                case 1:
                    allMods.Sort(CompareModProfilesByFileSize);
                    break;
            }


            //--------------------------------------------------------------------------------//
            //                                 DISPLAY MODS                                   //
            //--------------------------------------------------------------------------------//

            // Hide the existing collection items
            ListItem.HideListItems<CollectionModListItem>();

            bool hasSelection = false;
            string searchPhrase = CollectionPanelSearchField.text;
            CollectionModListItem lastItem = null;

            // GET LIST ITEMS TO SETUP
            foreach(var mod in allMods)
            {
                // check if the search phrase omits this mod id
                if(mod.name.IndexOf(searchPhrase, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                ListItem li = ListItem.GetListItem<CollectionModListItem>(CollectionPanelModListItem, CollectionPanelModListItemParent, SharedUi.colorScheme);

                if(li is CollectionModListItem item)
                {
                    li.Setup(mod);
                    li.SetViewportRestraint(CollectionPanelContentParent, null);
                    if(!hasSelection)
                    {
                        hasSelection = true;
                        InputNavigation.Instance.Select(li.selectable);
                        SetExplicitDownNavigationForTopRowButtons(li.selectable);
                        item.SetNavigationAbove(CollectionPanelCheckForUpdatesButton);
                    }

                    lastItem?.ConnectNavigationToItemBelow(item);
                    lastItem = item;
                }
            }

            if(!hasSelection)
            {
                InputNavigation.Instance.Select(defaultCollectionSelection);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(CollectionPanelModListItemParent as RectTransform);
        }

        /// <summary>
        /// This is to circumvent an odd behaviour when pressing 'down' to move the selection was
        /// jumping to the third, fourth or fifth list item in the collection panel list.
        /// </summary>
        /// <param name="selectable">This should be the first item in the collection list</param>
        void SetExplicitDownNavigationForTopRowButtons(Selectable selectable)
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

        public void OnScrollValueChange()
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
                collectionHeaderTransition = ImageTransitions.Alpha(CollectionPanelHeaderBackground, targetAlpha);
                StartCoroutine(collectionHeaderTransition);
            }
        }

        public void CheckForUpdates()
        {
            if(checkingForUpdates)
            {
                return;
            }
            Translation.Get(CollectionPanelCheckForUpdatesTextTranslation, "Checking...", CollectionPanelCheckForUpdatesText);
            ModIOUnity.FetchUpdates(FinishedCheckingForUpdates);
            checkingForUpdates = true;
        }

        void FinishedCheckingForUpdates(Result result)
        {
            checkingForUpdates = false;
            if(result.Succeeded())
            {
                RefreshList();
            }
            Translation.Get(CollectionPanelCheckForUpdatesTextTranslation, "Check for updates", CollectionPanelCheckForUpdatesText);
        }

        public string GetModNameFromId(ModId id)
        {
            // Get the name of this mod
            // check subscriptions
            foreach(var mod in subscribedMods)
            {
                if(mod.modProfile.id == id)
                {
                    return mod.modProfile.name;
                }
            }
            // check pending subscriptions
            foreach(var mod in pendingSubscriptions)
            {
                if(mod.id == id)
                {
                    return mod.name;
                }
            }
            return "A mod";
        }

        /// <summary>
        /// This is used to get the purchased, installed and subscribed mods and cache them for use across the UI
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

            // Get purchased Mods
            ModProfile[] purchased = ModIOUnity.GetPurchasedMods(out result);
            if(result.Succeeded())
            {
                purchasedMods = purchased;
            }
        }

        internal bool IsPurchased(ModProfile modProfile)
        {
            if(modProfile.price <= 0)
                return true;

            foreach(var m in purchasedMods)
            {
                if(m.id == modProfile.id)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool IsSubscribed(ModId id)
        {
            if(!Authentication.Instance.IsAuthenticated)
                return false;
            return IsSubscribed(id, out SubscribedModStatus _);
        }

        /// <summary>
        /// This will check if the given modId is subscribed and will also return the status of the
        /// specified mod, such as SubscribedModStatus.Installed
        /// </summary>
        /// <param name="id">The id of the mod to check</param>
        /// <param name="status">the out status of given modId</param>
        /// <returns>true if the mod is subscribed</returns>
        internal bool IsSubscribed(ModId id, out SubscribedModStatus status)
        {
            if(subscribedMods == null)
            {
                Instance.CacheLocalSubscribedModStatuses();
            }

            foreach(var mid in pendingUnsubscribes)
            {
                if(mid == id)
                {
                    status = SubscribedModStatus.None;
                    return false;
                }
            }
            foreach(var m in subscribedMods)
            {
                if(m.modProfile.id == id)
                {
                    status = m.status;
                    return true;
                }
            }
            foreach(var m in pendingSubscriptions)
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

        internal bool IsInstalled(ModId id)
        {
            if(installedMods == null)
            {
                CacheLocalSubscribedModStatuses();
            }

            foreach(var m in installedMods)
            {
                if(m.modProfile.id == id)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool GetSubscribedProfile(ModId id, out ModProfile profile)
        {
            if(subscribedMods == null)
            {
                Instance.CacheLocalSubscribedModStatuses();
            }

            foreach(var m in subscribedMods)
            {
                if(m.modProfile.id == id)
                {
                    profile = m.modProfile;
                    return true;
                }
            }
            foreach(var m in pendingSubscriptions)
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

        #region Comparer<T> delegates for sorting a List<ModProfile> via List<T>.Sort()
        static int CompareModProfilesAlphabetically(SubscribedMod A, SubscribedMod B)
        {
            return CompareModProfilesAlphabetically(A.modProfile, B.modProfile);
        }
        static int CompareModProfilesAlphabetically(InstalledMod A, InstalledMod B)
        {
            return CompareModProfilesAlphabetically(A.modProfile, B.modProfile);
        }
        static int CompareModProfilesAlphabetically(CollectionProfile A, CollectionProfile B)
        {
            return CompareModProfilesAlphabetically(A.modProfile, B.modProfile);
        }

        static int CompareModProfilesAlphabetically(ModProfile A, ModProfile B)
        {
            float valueOfA = 0;
            float valueOfB = 0;
            float depthMultiplier = 0;
            int maxDepth = 10;
            int depth = 0;

            foreach(char character in A.name)
            {
                if(depth >= maxDepth)
                {
                    break;
                }
                depthMultiplier = depthMultiplier == 0 ? 1 : depthMultiplier + 100;
                valueOfA += char.ToLower(character) / depthMultiplier;
                depth++;
            }

            depthMultiplier = 0;
            depth = 0;

            foreach(char character in B.name)
            {
                if(depth >= maxDepth)
                {
                    break;
                }
                depthMultiplier = depthMultiplier == 0 ? 1 : depthMultiplier + 100;
                valueOfB += char.ToLower(character) / depthMultiplier;
                depth++;
            }
            if(valueOfA > valueOfB)
            {
                return 1;
            }
            if(valueOfB > valueOfA)
            {
                return -1;
            }
            return 0;
        }

        static int CompareModProfilesByFileSize(InstalledMod A, InstalledMod B)
        {
            return CompareModProfilesByFileSize(A.modProfile, B.modProfile);
        }

        static int CompareModProfilesByFileSize(CollectionProfile A, CollectionProfile B)
        {
            return CompareModProfilesByFileSize(A.modProfile, B.modProfile);
        }

        static int CompareModProfilesByFileSize(ModProfile A, ModProfile B)
        {
            if(A.archiveFileSize > B.archiveFileSize)
            {
                return -1;
            }
            if(A.archiveFileSize < B.archiveFileSize)
            {
                return 1;
            }
            return 0;
        }
        #endregion Comparer<T> delegates for sorting a List<ModProfile> via List<T>.Sort()
    }
}
