using System;
using System.Collections.Generic;
using ModIO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// This is used with the CollectionModListItem prefabs to display the user's subscribed mods
    /// from the collection view.
    /// </summary>
    internal class CollectionModListItem : ListItem, ISelectHandler, IDeselectHandler
    {
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
        public Action imageLoaded;
        RectTransform rectTransform;

#pragma warning disable 0649 //they are allocated
        private Translation subscriptionStatusTranslation = null;
        private Translation installStatusTranslation = null;
        private Translation progressBarTextTranslation = null;
        private Translation otherSubscribersTextTranslation = null;
        private Translation errorInstallingTextTranslation = null;
#pragma warning restore 0649

        internal ModProfile profile;

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
            Browser.Instance.currentSelectedCollectionListItem = this;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if(Browser.Instance.currentSelectedCollectionListItem == this)
            {
                Browser.Instance.currentSelectedCollectionListItem = null;
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
            Browser.SelectSelectable(listItemButton);
        }

        public override void SetViewportRestraint(RectTransform content, RectTransform viewport)
        {
            base.SetViewportRestraint(content, viewport);

            viewportRestraint.PercentPaddingVertical = 0.35f;
        }

        

        public override void Setup(InstalledMod profile)
        {
            base.Setup();
            this.profile = profile.modProfile;

            Translation.Get(subscriptionStatusTranslation, "Installed", subscriptionStatus);
            subscriptionStatus.color = scheme.LightGrey1;

            Translation.Get(installStatusTranslation, "Installed", installStatus);
            Translation.Get(otherSubscribersTextTranslation, "{subcount} other users", otherSubscribersText, $"{profile.subscribedUsers.Count}");
            otherSubscribersText.transform.parent.gameObject.SetActive(true);
            unsubscribeButton.gameObject.SetActive(false);
            progressBar.SetActive(false);
            Hydrate();
        }

        public override void Setup(ModProfile profile, bool subscriptionStatus, string installationStatus)
        {
            base.Setup();
            this.profile = profile;

            if(subscriptionStatus)
            {
                Translation.Get(subscriptionStatusTranslation, "Subscribed", this.subscriptionStatus);
            }
            else
            {
                Translation.Get(subscriptionStatusTranslation, "Unsubscribed", this.subscriptionStatus);
            }

            this.subscriptionStatus.color = subscriptionStatus ? scheme.Green : scheme.LightGrey1;
            if(installationStatus == "Problem occurred")
            {
                installStatus.gameObject.SetActive(false);
                errorInstalling.SetActive(true);

                if(Browser.Instance.notEnoughSpaceForTheseMods.Contains(profile.id))
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
                Translation.Get(installStatusTranslation, installationStatus, installStatus);
            }
            unsubscribeButton.gameObject.SetActive(true);
            progressBar.SetActive(false);
            otherSubscribersText.transform.parent.gameObject.SetActive(false);
            Hydrate();
        }
#endregion

        void AddToStaticDictionaryCache()
        {
            if(listItems.ContainsKey(profile.id))
            {
                listItems[profile.id] = this;
            }
            else
            {
                listItems.Add(profile.id, this);
            }
        }

        void Hydrate()
        {
            AddToStaticDictionaryCache();
            failedToLoadLogo.SetActive(false);
            imageBackground.gameObject.SetActive(false);
            title.text = profile.name;
            fileSize.text = Utility.GenerateHumanReadableStringForBytes(profile.archiveFileSize);
            ModIOUnity.DownloadTexture(profile.logoImage_320x180, SetIcon);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            RedrawRectTransform();
        }

        public void OpenModDetailsForThisProfile()
        {
            if(isPlaceholder)
            {
                return;
            }
            Browser.Instance.OpenModDetailsPanel(profile, Browser.Instance.OpenModCollection);
        }

        void RemoveFromStaticDictionaryCache()
        {
            if(listItems.ContainsKey(profile.id))
            {
                listItems.Remove(profile.id);
            }
        }

        void SetIcon(ResultAnd<Texture2D> textureAnd)
        {
            if(textureAnd.result.Succeeded() && textureAnd.value != null)
            {
                imageBackground.gameObject.SetActive(true);
                image.sprite = Sprite.Create(textureAnd.value, new Rect(Vector2.zero, new Vector2(textureAnd.value.width, textureAnd.value.height)), Vector2.zero);
            }
            else
            {
                failedToLoadLogo.SetActive(true);
            }
            imageLoaded?.Invoke();
        }

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
                    ModIOUnity.RateMod(profile.id, ModRating.Positive, delegate { });
                    Browser.Instance.CloseContextMenu();
                }
            });

            // Add Vote up option to context menu
            options.Add(new ContextMenuOption
            {
                nameTranslationReference = "Vote down",
                action = delegate
                {
                    ModIOUnity.RateMod(profile.id, ModRating.Negative, delegate { });
                    Browser.Instance.CloseContextMenu();
                }
            });

            // Add Report option to context menu
            options.Add(new ContextMenuOption
            {
                nameTranslationReference = "Report",
                action = delegate
                {
                    Browser.Instance.CloseContextMenu();
                    Browser.Instance.OpenReportPanel(profile, selectable);
                }
            });

            // Open context menu
            Browser.Instance.OpenContextMenu(contextMenuPosition, options, listItemButton);
        }

        public void UnsubscribeButton()
        {
            // TODO add 'subscribe' alternate for installed mods
            Browser.Instance.OpenUninstallConfirmation(profile);
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
