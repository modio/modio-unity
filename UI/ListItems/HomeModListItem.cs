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
    /// This is used for the BrowserModListItem prefab for the home view rows, such as
    /// recently added, highest rated, etc
    /// </summary>
    /// <remarks>
    /// This is nearly identical to SearchResultListItem.cs due to the potential of any future
    /// design changes if we ever want them to be more distinguished.
    /// </remarks>
    internal class HomeModListItem : ListItem, IDeselectHandler, ISelectHandler, IPointerEnterHandler
    {
        public Image image;
        public TMP_Text titleTxt;
        public GameObject loadingIcon;
        public GameObject failedToLoadIcon;
        public Action imageLoaded;
        public SubscribedProgressTab progressTab;
        internal Translation priceTranslation = null;

        internal static Dictionary<ModId, HomeModListItem> listItems = new Dictionary<ModId, HomeModListItem>();

        // TODO This may need to be implemented with mouse & keyboard support
        public void OpenModDetailsForThisProfile()
        {
            if(isPlaceholder)
            {
                return;
            }
            Details.Instance.Open(profile, Home.Instance.Open);
        }

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

        void RemoveFromStaticDictionaryCache()
        {
            if(listItems.ContainsKey(profile.id))
            {
                listItems.Remove(profile.id);
            }
        }

#region MonoBehaviour
        void OnDestroy()
        {
            RemoveFromStaticDictionaryCache();
        }

        public void OnSelect(BaseEventData eventData)
        {
            SelectionOverlayHandler.Instance.MoveSelection(this);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SelectionOverlayHandler.Instance.Deselect(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // When using mouse we want to disable the viewport restraint from moving the screen
            InputNavigation.Instance.mouseNavigation = true;

            EventSystem.current.SetSelectedGameObject(null);
            InputNavigation.Instance.Select(selectable, true);
        }
#endregion // MonoBehaviour

#region Overrides

        public override void PlaceholderSetup()
        {
            base.PlaceholderSetup();
            image.color = Color.clear;
            loadingIcon.SetActive(true);
            failedToLoadIcon.SetActive(false);
            titleTxt.text = string.Empty;
            //downloads.text = string.Empty;
        }

        public override void Setup(ModProfile modProfile)
        {
            base.Setup();
            profile = modProfile;
            image.color = Color.clear;
            loadingIcon.SetActive(true);
            failedToLoadIcon.SetActive(false);
            titleTxt.text = modProfile.name;

            //downloads.text = GenerateHumanReadableString(profile.stats.downloadsTotal);
            ModIOUnity.DownloadTexture(modProfile.logoImage320x180, SetIcon);
            gameObject.SetActive(true);

            progressTab.Setup(modProfile);

            AddToStaticDictionaryCache();
        }

        public override void SetViewportRestraint(RectTransform content, RectTransform viewport)
        {
            base.SetViewportRestraint(content, viewport);

            viewportRestraint.adjustVertically = false;
            viewportRestraint.adjustHorizontally = true;
            viewportRestraint.PercentPaddingHorizontal = 0.025f;
        }

#endregion // Overrides

        void SetIcon(ResultAnd<Texture2D> resultAndTexture)
        {
            if(resultAndTexture.result.Succeeded() && resultAndTexture != null)
            {
                QueueRunner.Instance.AddSpriteCreation(resultAndTexture.value, sprite =>
                {
                    image.sprite = sprite;
                    image.color = Color.white;
                    loadingIcon.SetActive(false);
                });
            }
            else
            {
                failedToLoadIcon.SetActive(true);
                loadingIcon.SetActive(false);
            }
            imageLoaded?.Invoke();
        }

        internal void UpdateProgressBar(ProgressHandle handle)
        {
            progressTab.UpdateProgress(handle);
        }

        internal void UpdateStatus(ModManagementEventType updatedStatus, ModId id)
        {
            progressTab.UpdateStatus(updatedStatus, id);
        }
    }
}
