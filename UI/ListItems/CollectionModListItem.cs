using System;
using System.Collections.Generic;
using ModIO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ModIO.Util;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// This is used with the CollectionModListItem prefabs to display the user's subscribed mods
    /// from the collection view.
    /// </summary>
    internal class CollectionModListItem : ListItem, ISelectHandler, IDeselectHandler
    {
        new CollectionProfile profile;

        [SerializeField] Button listItemButton;
        [SerializeField] Image image;
        [SerializeField] GameObject imageBackground;
        [SerializeField] TMP_Text title;
        [SerializeField] GameObject progressBar;
        [SerializeField] Image progressBarFill;
        [SerializeField] TMP_Text progressBarText;
        [SerializeField] TMP_Text progressBarPercentageText;
        [SerializeField] TMP_Text subscriptionStatus;
        [SerializeField] TMP_Text installStatus;
        [SerializeField] TMP_Text fileSize;
        [SerializeField] Button unsubscribeButton;
        [SerializeField] TMP_Text otherSubscribersText;
        [SerializeField] Button moreOptionsButton;
        [SerializeField] GameObject failedToLoadLogo;
        [SerializeField] GameObject errorInstalling;
        [SerializeField] TMP_Text errorInstallingText;
        [SerializeField] Transform contextMenuPosition;
        [SerializeField] MultiTargetToggle enabledOrDisabledToggle;
        ViewportRestraint togglesViewportRestraint;
        [SerializeField] GameObject disabledBlackOverlay;
        public Action imageLoaded;
        RectTransform rectTransform;

#pragma warning disable 0649 //they are allocated
        Translation subscriptionStatusTranslation = null;
        Translation installStatusTranslation = null;
        Translation progressBarTextTranslation = null;
        Translation otherSubscribersTextTranslation = null;
        Translation errorInstallingTextTranslation = null;
#pragma warning restore 0649

        internal static Dictionary<ModId, CollectionModListItem> listItems = new Dictionary<ModId, CollectionModListItem>();

#region Monobehaviour
        void OnEnable()
        {
            rectTransform = transform as RectTransform;
        }

        void OnDestroy()
        {
            RemoveFromStaticDictionaryCache();
        }

        public void OnSelect(BaseEventData eventData)
        {
            Collection.Instance.currentSelectedCollectionListItem = this;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if(Collection.Instance.currentSelectedCollectionListItem == this)
            {
                Collection.Instance.currentSelectedCollectionListItem = null;
            }
        }
#endregion

#region Overrides
        public override void PlaceholderSetup()
        {
            base.PlaceholderSetup();
            failedToLoadLogo.SetActive(false);
            imageBackground.gameObject.SetActive(false);
            title.text = string.Empty;
            //downloads.text = string.Empty;
        }

        public override void Select()
        {
            InputNavigation.Instance.Select(listItemButton);
        }

        public override void SetViewportRestraint(RectTransform content, RectTransform viewport)
        {
            base.SetViewportRestraint(content, viewport);

            // We add a viewport restraint to the toggle as well
            if(togglesViewportRestraint == null)
            {
                togglesViewportRestraint = enabledOrDisabledToggle.gameObject.AddComponent<ViewportRestraint>();
            }
            togglesViewportRestraint.DefaultViewportContainer = content;
            togglesViewportRestraint.Viewport = viewport;

            togglesViewportRestraint.PercentPaddingVertical = 0.35f;
            viewportRestraint.PercentPaddingVertical = 0.35f;
        }

        public override void Setup(CollectionProfile profile)
        {
            base.Setup();
            this.profile = profile;

            // Enabled or Disabled Toggle
            SetupEnableDisableToggle();

            // Subscribed or Number of subscribers text
            SetupSubscribedStatusText();

            // Installed / Downloading text
            SetupInstallationStatusText();

            // Set explicit navigation between toggle and the list item button itself
            SetupNavigationBetweenToggleAndListItem();

            // Deactivate button if not subscribed
            unsubscribeButton.gameObject.SetActive(profile.subscribed);

            // Always set the progress bar to off by default
            progressBar.SetActive(false);

            Hydrate();
        }

#endregion
        void SetupSubscribedStatusText()
        {
            if(profile.subscribed)
            {
                Translation.Get(subscriptionStatusTranslation, "Subscribed", subscriptionStatus);
                subscriptionStatus.color = scheme.PositiveAccent;
            }
            else
            {
                Translation.Get(subscriptionStatusTranslation, "Installed", subscriptionStatus);
                subscriptionStatus.color = scheme.Inactive1;

                Translation.Get(otherSubscribersTextTranslation, "{subcount} other users", otherSubscribersText, $"{profile.subscribers}");
            }
            otherSubscribersText.transform.parent.gameObject.SetActive(!profile.subscribed);
        }

        void SetupEnableDisableToggle()
        {
            if (profile.subscribed)
            {
                enabledOrDisabledToggle.onValueChanged.RemoveAllListeners();
                enabledOrDisabledToggle.isOn = profile.enabled;
                enabledOrDisabledToggle.interactable = true;
                enabledOrDisabledToggle.onValueChanged.AddListener(ToggleModEnabled);
            }
            else
            {
                enabledOrDisabledToggle.interactable = false;
            }
            enabledOrDisabledToggle.DoStateTransition();
        }

        void SetupNavigationBetweenToggleAndListItem()
        {
            Navigation navigation = listItemButton.navigation;
            navigation.selectOnLeft = enabledOrDisabledToggle.interactable ? enabledOrDisabledToggle : null;
            listItemButton.navigation = navigation;

            navigation = enabledOrDisabledToggle.navigation;
            navigation.selectOnRight = listItemButton;
            enabledOrDisabledToggle.navigation = navigation;
        }

        void ToggleModEnabled(bool enabled)
        {
            if(enabled)
            {
                EnableMod();
            }
            else
            {
                DisabledMod();
            }

            SetDisabledStateOverlay();
            enabledOrDisabledToggle.DoStateTransition();
        }

        /// <summary>
        /// This should only be used for the very top item in the list to move onto the
        /// 'Check for updates' button, for example
        /// </summary>
        /// <param name="above">the button above</param>
        public void SetNavigationAbove(Selectable above)
        {
            Navigation navigation = listItemButton.navigation;
            navigation.selectOnUp = above;
            listItemButton.navigation = navigation;

            navigation = enabledOrDisabledToggle.navigation;
            navigation.selectOnUp = above;
            enabledOrDisabledToggle.navigation = navigation;
        }

        public void ConnectNavigationToItemBelow(CollectionModListItem below)
        {
            // Set navigation down to the item beneath this one
            Navigation navigation = listItemButton.navigation;
            navigation.selectOnDown = below.listItemButton;
            listItemButton.navigation = navigation;

            navigation = enabledOrDisabledToggle.navigation;
            navigation.selectOnDown = below.enabledOrDisabledToggle.interactable
                ? (Selectable)below.enabledOrDisabledToggle : below.listItemButton;
            enabledOrDisabledToggle.navigation = navigation;

            // Set navigation up for the item beneath this one
            navigation = below.listItemButton.navigation;
            navigation.selectOnUp = listItemButton;
            below.listItemButton.navigation = navigation;

            navigation = below.enabledOrDisabledToggle.navigation;
            navigation.selectOnUp = enabledOrDisabledToggle.interactable
                ? (Selectable)enabledOrDisabledToggle : listItemButton;
            below.enabledOrDisabledToggle.navigation = navigation;
        }

        void EnableMod()
        {
            if(ModIOUnity.EnableMod(profile.modProfile.id))
            {
                profile.enabled = true;
            }
        }

        void DisabledMod()
        {
            if(ModIOUnity.DisableMod(profile.modProfile.id))
            {
                profile.enabled = false;
            }
        }

        void SetupInstallationStatusText()
        {
            // If not subscribed
            if(!profile.subscribed)
            {
                Translation.Get(installStatusTranslation, "Installed", installStatus);
                return;
            }

            // If subscribed
            if(profile.installationStatus == "Problem occurred")
            {
                installStatus.gameObject.SetActive(false);
                errorInstalling.SetActive(true);

                if(Collection.Instance.notEnoughSpaceForTheseMods.Contains(profile.modProfile.id))
                {
                    Translation.Get(errorInstallingTextTranslation, "Full storage", errorInstallingText);
                }
                else
                {
                    Translation.Get(errorInstallingTextTranslation, "Error", errorInstallingText);
                }
            }
            else
            {
                installStatus.gameObject.SetActive(true);
                errorInstalling.SetActive(false);
                Translation.Get(installStatusTranslation, profile.installationStatus, installStatus);
            }
        }

        void AddToStaticDictionaryCache()
        {
            if(listItems.ContainsKey(profile.modProfile.id))
            {
                listItems[profile.modProfile.id] = this;
            }
            else
            {
                listItems.Add(profile.modProfile.id, this);
            }
        }

        void Hydrate()
        {
            AddToStaticDictionaryCache();
            failedToLoadLogo.SetActive(false);
            imageBackground.gameObject.SetActive(false);
            title.text = profile.modProfile.name;
            fileSize.text = Utility.GenerateHumanReadableStringForBytes(profile.modProfile.archiveFileSize);
            ModIOUnity.DownloadTexture(profile.modProfile.logoImage320x180, SetIcon);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            SetDisabledStateOverlay();
            RedrawRectTransform();
        }

        public void OpenModDetailsForThisProfile()
        {
            if(isPlaceholder)
            {
                return;
            }
            Details.Instance.Open(profile.modProfile, Collection.Instance.Open);
        }

        void RemoveFromStaticDictionaryCache()
        {
            if(listItems.ContainsKey(profile.modProfile.id))
            {
                listItems.Remove(profile.modProfile.id);
            }
        }

        void SetIcon(ResultAnd<Texture2D> textureAnd)
        {
            if(textureAnd.result.Succeeded() && textureAnd.value != null)
            {
                QueueRunner.Instance.AddSpriteCreation(textureAnd.value, sprite =>
                {
                    imageBackground.gameObject.SetActive(true);
                    image.sprite = sprite;
                });
            }
            else
            {
                failedToLoadLogo.SetActive(true);
            }
            imageLoaded?.Invoke();
        }

        /// <summary>
        /// Turns the black overlay on or off depending on the state
        /// </summary>
        void SetDisabledStateOverlay() => disabledBlackOverlay.SetActive(profile.subscribed && !profile.enabled);


        public void ShowMoreOptions()
        {
            List<ContextMenuOption> options = new List<ContextMenuOption>();

            //TODO If not subscribed add force uninstall and subscribe options

            // Add Vote up option to context menu
            options.Add(new ContextMenuOption
            {
                nameTranslationReference = "Vote up",
                action = delegate
                {
                    ModIOUnity.RateMod(profile.modProfile.id, ModRating.Positive, delegate { });
                    ModioContextMenu.Instance.Close();
                }
            });

            // Add Vote up option to context menu
            options.Add(new ContextMenuOption
            {
                nameTranslationReference = "Vote down",
                action = delegate
                {
                    ModIOUnity.RateMod(profile.modProfile.id, ModRating.Negative, delegate { });
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
                    Reporting.Instance.Open(profile.modProfile, selectable);
                }
            });

            if (!profile.subscribed)
            {
                // Add Uninstall option to context menu
                options.Add(new ContextMenuOption
                {
                    nameTranslationReference = "Uninstall",
                    action = delegate
                    {
                        ModioContextMenu.Instance.Close();
                        ForceUninstall();
                    }
                });
            }

            // Open context menu
            ModioContextMenu.Instance.Open(contextMenuPosition, options, listItemButton);
        }

        void ForceUninstall()
        {
            Result result = ModIOUnity.ForceUninstallMod(profile.modProfile.id);
            if(result.Succeeded())
            {
                Notifications.Instance.AddNotificationToQueue(new Notifications.QueuedNotice
                {
                    title = "Uninstalled",
                    description = $"Uninstalled the mod '{profile.modProfile.name}'",
                    positiveAccent = true
                });
                gameObject.SetActive(false);
            }
            else
            {
                Notifications.Instance.AddNotificationToQueue(new Notifications.QueuedNotice
                {
                    title = "Failed to uninstall",
                    description = $"Failed to uninstall the mod '{profile.modProfile.name}'",
                    positiveAccent = false
                });
            }
        }

        public void UnsubscribeButton()
        {
            // TODO add 'subscribe' alternate for installed mods
            Collection.Instance.OpenUninstallConfirmation(profile.modProfile);
        }

        internal void UpdateStatus(ModManagementEventType updatedStatus)
        {
            // Always turn this off when state changes. It will auto get turned back on if needed
            progressBar.SetActive(false);
            errorInstalling.SetActive(false);
            installStatus.gameObject.SetActive(true);

            switch(updatedStatus)
            {
                case ModManagementEventType.InstallStarted:
                    Translation.Get(installStatusTranslation, "Installing", installStatus);
                    break;
                case ModManagementEventType.Installed:
                    Translation.Get(installStatusTranslation, "Installed", installStatus);
                    break;
                case ModManagementEventType.InstallFailed:
                    installStatus.gameObject.SetActive(false);
                    errorInstalling.SetActive(true);
                    break;
                case ModManagementEventType.DownloadStarted:
                    Translation.Get(installStatusTranslation, "Downloading", installStatus);
                    break;
                case ModManagementEventType.Downloaded:
                    Translation.Get(installStatusTranslation, "Ready to install", installStatus);
                    break;
                case ModManagementEventType.DownloadFailed:
                    installStatus.gameObject.SetActive(false);
                    errorInstalling.SetActive(true);
                    break;
                case ModManagementEventType.UninstallStarted:
                    Translation.Get(installStatusTranslation, "Uninstalling", installStatus);
                    break;
                case ModManagementEventType.Uninstalled:
                    Translation.Get(installStatusTranslation, "Uninstalled", installStatus);
                    break;
                case ModManagementEventType.UninstallFailed:
                    installStatus.gameObject.SetActive(false);
                    errorInstalling.SetActive(true);
                    break;
                case ModManagementEventType.UpdateStarted:
                    Translation.Get(installStatusTranslation, "Updating", installStatus);
                    break;
                case ModManagementEventType.Updated:
                    Translation.Get(installStatusTranslation, "Updated", installStatus);
                    break;
                case ModManagementEventType.UpdateFailed:
                    installStatus.gameObject.SetActive(false);
                    errorInstalling.SetActive(true);
                    break;
            }
        }

        internal void UpdateProgressState(ProgressHandle handle)
        {
            if(handle == null || handle.Completed)
            {
                progressBar.SetActive(false);
                return;
            }

            progressBarFill.fillAmount = handle.Progress;

            switch(handle.OperationType)
            {
                case ModManagementOperationType.None_AlreadyInstalled:
                    progressBar.SetActive(false);
                    installStatus.gameObject.SetActive(true);
                    Translation.Get(installStatusTranslation, "Installed", installStatus);
                    break;
                case ModManagementOperationType.None_ErrorOcurred:
                    progressBar.SetActive(false);
                    installStatus.gameObject.SetActive(false);
                    errorInstalling.SetActive(true);
                    break;
                case ModManagementOperationType.Install:
                    progressBar.SetActive(true);
                    installStatus.gameObject.SetActive(false);

                    progressBarPercentageText.text = $"{(int)(handle.Progress * 100)}%";
                    Translation.Get(progressBarTextTranslation, "Installing...", progressBarText);
                    break;
                case ModManagementOperationType.Download:
                    progressBar.SetActive(true);
                    installStatus.gameObject.SetActive(false);
                    progressBarPercentageText.text = $"{(int)(handle.Progress * 100)}%";
                    Translation.Get(progressBarTextTranslation, "Downloading...", progressBarText);
                    break;
                case ModManagementOperationType.Uninstall:
                    progressBar.SetActive(false);
                    installStatus.gameObject.SetActive(true);
                    Translation.Get(progressBarTextTranslation, "Uninstalling", progressBarText);
                    break;
                case ModManagementOperationType.Update:
                    progressBar.SetActive(true);
                    installStatus.gameObject.SetActive(false);
                    progressBarPercentageText.text = $"{(int)(handle.Progress * 100)}%";
                    Translation.Get(progressBarTextTranslation, "Updating...", progressBarText);
                    break;
            }
        }
    }
}
