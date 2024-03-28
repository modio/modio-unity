using System.Collections.Generic;
using ModIO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace ModIOBrowser.Implementation
{
    internal class SearchResultListItem_Overlay : MonoBehaviour, IPointerExitHandler
    {
        [SerializeField] Animator animator;
        [SerializeField] Image image;
        [SerializeField] GameObject failedToLoadIcon;
        [SerializeField] GameObject loadingIcon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subscribeButtonText;
        [SerializeField] Transform contextMenuPosition;

        public SearchResultListItem listItemToReplicate;
        public SearchResultListItem lastListItemToReplicate;

#pragma warning disable 0649 //it is allocated
        private Translation subscribeButtonTextTranslation;
#pragma warning restore 0649

        [SerializeField] SubscribedProgressTab progressTab;

        void LateUpdate()
        {
            if(gameObject.activeSelf)
            {
                MimicProgressBar();
            }
        }

        // Mimics the look of a SearchResultListItem
        public void Setup(SearchResultListItem listItem)
        {
            // If we are already displaying this list item in the overlay, bail
            // if(listItem == listItemToReplicate && gameObject.activeSelf)
            // {
            //     return;
            // }
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
            title.text = listItemToReplicate.title.text;
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

            if(!Collection.Instance.IsPurchased(listItemToReplicate.profile))
            {
                Translation.Get(subscribeButtonTextTranslation, "Buy Now", subscribeButtonText);
            }
            else if(Collection.Instance.IsSubscribed(listItemToReplicate.profile.id))
            {
                Translation.Get(subscribeButtonTextTranslation, "Unsubscribe", subscribeButtonText);
            }
            else
            {
                Translation.Get(subscribeButtonTextTranslation, "Subscribe", subscribeButtonText);
            }
        }

        void ReloadImage()
        {
            image.sprite = listItemToReplicate.image.sprite;
            failedToLoadIcon.SetActive(listItemToReplicate.failedToLoadIcon.activeSelf);
            loadingIcon.SetActive(listItemToReplicate.loadingIcon.activeSelf);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!ModioContextMenu.Instance.ContextMenu.activeSelf)
            {
                InputNavigation.Instance.DeselectUiGameObject();
                gameObject.SetActive(false);
            }
        }
    }
}
