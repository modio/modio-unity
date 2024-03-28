using System;
using System.Collections.Generic;
using ModIO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    internal class HomeModListItem_Overlay : MonoBehaviour, IPointerExitHandler
    {
        [SerializeField] Animator animator;
        [SerializeField] Image image;
        [SerializeField] GameObject failedToLoadIcon;
        [SerializeField] GameObject loadingIcon;
        [SerializeField] TMP_Text titleTxt;
        [SerializeField] TMP_Text subscribeButtonText;
        [SerializeField] Transform contextMenuPosition;

        public HomeModListItem listItemToReplicate;
        public HomeModListItem lastListItemToReplicate;
        [SerializeField] SubscribedProgressTab progressTab;
        internal Translation subscribeButtonTextTranslation = null;

        void LateUpdate()
        {
            if(gameObject.activeSelf)
            {
                MimicProgressBar();
            }
        }

        // Mimics the look of a BrowserModListItem
        public void Setup(HomeModListItem listItem)
        {
            // If we are already displaying this list item in the overlay, bail
            lastListItemToReplicate = listItemToReplicate;
            listItemToReplicate = listItem;

            Transform t = transform;
            t.SetParent(listItem.transform.parent);
            t.SetAsLastSibling();
            t.position = listItem.transform.position;
            gameObject.SetActive(true);
            failedToLoadIcon.SetActive(listItemToReplicate.failedToLoadIcon.activeSelf);
            loadingIcon.SetActive(listItemToReplicate.loadingIcon.activeSelf);
            animator.Play("Inflate");
            SetSubscribeButtonText();

            MimicProgressBar();

            // Set if the list item is still waiting for the image to download. The action will
            // get invoked when the download finishes.
            listItemToReplicate.imageLoaded = ReloadImage;

            image.sprite = listItemToReplicate.image.sprite;
            titleTxt.text = listItemToReplicate.titleTxt.text;
        }

        void MimicProgressBar()
        {
            if (listItemToReplicate != null)
            {
                progressTab?.MimicOtherProgressTab(listItemToReplicate?.progressTab);
            }
        }

        public void SubscribeButton()
        {
            if(Collection.Instance.IsSubscribed(listItemToReplicate.profile.id))
            {
                // We are pre-emptively changing the text here to make the UI feel more responsive
                Translation.Get(subscribeButtonTextTranslation, "Unsubscribe", subscribeButtonText);
                Mods.UnsubscribeFromEvent(listItemToReplicate.profile, UpdateSubscribeButton);
            }
            else
            {
                // We are pre-emptively changing the text here to make the UI feel more responsive
                Translation.Get(subscribeButtonTextTranslation, "Subscribe", subscribeButtonText);
                Mods.SubscribeToEvent(listItemToReplicate.profile, UpdateSubscribeButton);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(subscribeButtonText.transform.parent as RectTransform);
        }

        public void OpenModDetailsForThisModProfile()
        {
            listItemToReplicate?.OpenModDetailsForThisProfile();
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
                    Home.RateMod(listItemToReplicate.profile.id, ModRating.Positive);
                    ModioContextMenu.Instance.Close();
                }
            });

            // Add Vote up option to context menu
            options.Add(new ContextMenuOption
            {
                nameTranslationReference = "Vote down",
                action = delegate
                {
                    Home.RateMod(listItemToReplicate.profile.id, ModRating.Negative);
                    ModioContextMenu.Instance.Close();
                }
            });

            // Add Report option to context menu
            options.Add(new ContextMenuOption
            {
                nameTranslationReference = "Report",
                action = delegate
                {
                    // TODO open report menu
                    ModioContextMenu.Instance.Close();
                    Reporting.Instance.Open(listItemToReplicate.profile, listItemToReplicate.selectable);
                }
            });

            // Open context menu
            ModioContextMenu.Instance.Open(contextMenuPosition, options, listItemToReplicate.selectable);
        }

        public void UpdateSubscribeButton()
        {
            SetSubscribeButtonText();
        }

        public void SetSubscribeButtonText()
        {
            listItemToReplicate?.progressTab?.Setup(listItemToReplicate.profile);

            if(Collection.Instance.IsSubscribed(listItemToReplicate.profile.id))
            {
                Translation.Get(subscribeButtonTextTranslation, "Unsubscribe", subscribeButtonText);
            }
            else
            {
                Translation.Get(subscribeButtonTextTranslation, "Subscribe", subscribeButtonText);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(subscribeButtonText.transform.parent as RectTransform);
        }

        void ReloadImage()
        {
            image.sprite = listItemToReplicate.image.sprite;
            failedToLoadIcon.SetActive(listItemToReplicate.failedToLoadIcon.activeSelf);
            loadingIcon.SetActive(listItemToReplicate.loadingIcon.activeSelf);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!ModioContextMenu.Instance.gameObject.activeSelf)
            {
                InputNavigation.Instance.DeselectUiGameObject();
                gameObject.SetActive(false);
            }
        }
    }
}
