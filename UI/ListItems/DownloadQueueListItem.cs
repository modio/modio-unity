using ModIO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ModIO.Util;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// This is used for the queued mods that haven't yet started downloading, displayed in the list
    /// in the download queue panel.
    /// </summary>
    internal class DownloadQueueListItem : ListItem, IDeselectHandler, ISelectHandler
    {
        [SerializeField] TMP_Text modName;
        [SerializeField] TMP_Text fileSize;
        [SerializeField] Image modLogo;
        [SerializeField] GameObject loadingIcon;
        [SerializeField] GameObject failedToLoadIcon;
        [SerializeField] GameObject failedToLoadMod;

        public static DownloadQueueListItem currentDownloadQueueListItem;

        // TODO @Steve this may have to be hooked up for mouse & keyboard support
        public void OpenModDetailsForThisProfile()
        {
            Details.Instance.Open(profile, delegate { DownloadQueue.Instance.OpenDownloadQueuePanel(); });
        }

#region Overrides
        public override void SetViewportRestraint(RectTransform content, RectTransform viewport)
        {
            base.SetViewportRestraint(content, viewport);
        }

        public override void Setup(SubscribedMod mod)
        {
            base.Setup();
            this.profile = mod.modProfile;
            modName.text = mod.modProfile.name;
            fileSize.text = Utility.GenerateHumanReadableStringForBytes(mod.modProfile.archiveFileSize);
            failedToLoadMod.SetActive(mod.status == SubscribedModStatus.ProblemOccurred);
            modLogo.color = Color.clear;
            gameObject.SetActive(true);
            failedToLoadIcon.SetActive(false);
            loadingIcon.SetActive(true);
            ModIOUnity.DownloadTexture(mod.modProfile.logoImage320x180, SetIcon);

            LayoutRebuilder.ForceRebuildLayoutImmediate(modName.transform.parent as RectTransform);
        }
#endregion // Overrides

        void SetIcon(ResultAnd<Texture2D> textureAnd)
        {
            if(textureAnd.result.Succeeded() && textureAnd.value != null)
            {
                QueueRunner.Instance.AddSpriteCreation(textureAnd.value, sprite =>
                {
                    modLogo.color = Color.white;
                    modLogo.sprite = sprite;
                });
            }
            else
            {
                failedToLoadIcon.SetActive(true);
            }
            loadingIcon.SetActive(false);
        }

        public void Unsubscribe()
        {
            Mods.UnsubscribeFromEvent(profile);
            DownloadQueue.Instance.RefreshDownloadHistoryPanel();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if(currentDownloadQueueListItem == this)
            {
                currentDownloadQueueListItem = null;
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            currentDownloadQueueListItem = this;
        }
    }
}
