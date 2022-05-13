using ModIO;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

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
        public ModProfile profile;

        public static DownloadQueueListItem currentDownloadQueueListItem;

        // TODO @Steve this may have to be hooked up for mouse & keyboard support
        public void OpenModDetailsForThisProfile()
        {
            Browser.Instance.OpenModDetailsPanel(profile, delegate { Browser.Instance.OpenDownloadQueuePanel(); });
        }
        
#region Overrides
        public override void SetViewportRestraint(RectTransform content, RectTransform viewport)
        {
            base.SetViewportRestraint(content, viewport);
            viewportRestraint.UseScreenAsViewport = false;
        }

        public override void Setup(ModProfile profile)
        {
            base.Setup();
            this.profile = profile;
            modName.text = profile.name;
            fileSize.text = ""; // TODO @Steve Implement this
            gameObject.SetActive(true);
        }
#endregion // Overrides
        
        public void Unsubscribe()
        {
            Browser.UnsubscribeFromModEvent(profile);
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
