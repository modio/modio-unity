using System;
using System.Collections;
using System.Collections.Generic;
using ModIO;
using ModIOBrowser.Implementation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModIOBrowser
{
    /// <summary>
    ///the main interface for interacting with the Mod Browser UI
    /// </summary>
    public partial class Browser
    {
        [Header("Mod Details Panel")]
        [SerializeField] GameObject ModDetailsPanel;
        [SerializeField] RectTransform ModDetailsContentRect;
        [SerializeField] GameObject ModDetailsGalleryLoadingAnimation;
        [SerializeField] Image ModDetailsGalleryFailedToLoadIcon;
        [SerializeField] Image[] ModDetailsGalleryImage;
        [SerializeField] TMP_Text ModDetailsSubscribeButtonText;
        [SerializeField] TMP_Text ModDetailsName;
        [SerializeField] TMP_Text ModDetailsSummary;
        [SerializeField] TMP_Text ModDetailsDescription;
        [SerializeField] TMP_Text ModDetailsFileSize;
        [SerializeField] TMP_Text ModDetailsLastUpdated;
        [SerializeField] TMP_Text ModDetailsReleaseDate;
        [SerializeField] TMP_Text ModDetailsSubscribers;
        [SerializeField] TMP_Text ModDetailsCreatedBy;
        [SerializeField] TMP_Text ModDetailsUpVotes;
        [SerializeField] TMP_Text ModDetailsDownVotes;
        [SerializeField] GameObject ModDetailsGalleryNavBar;
        [SerializeField] Transform ModDetailsGalleryNavButtonParent;
        [SerializeField] GameObject ModDetailsGalleryNavButtonPrefab;
        [SerializeField] GameObject ModDetailsDownloadProgressDisplay;
        [SerializeField] Image ModDetailsDownloadProgressFill;
        [SerializeField] TMP_Text ModDetailsDownloadProgressRemaining;
        [SerializeField] TMP_Text ModDetailsDownloadProgressSpeed;
        [SerializeField] TMP_Text ModDetailsDownloadProgressCompleted;
        [SerializeField] SubscribedProgressTab ModDetailsProgressTab;
        [SerializeField] GameObject ModDetailsScrollToggleGameObject;
        bool galleryImageInUse;
        Sprite[] ModDetailsGalleryImages;
        bool[] ModDetailsGalleryImagesFailedToLoad;
        int galleryPosition;
        float galleryTransitionTime = 0.3f;
        IEnumerator galleryTransition;
        ModProfile currentModProfileBeingViewed;
        IEnumerator downloadProgressUpdater;
        
        // We set this action from where the panel is opened so we can also set which panel to re-open when this details panel is closed
        Action modDetailsOnCloseAction;
        
        // measuring the progress bar
        ModId detailsModIdOfLastProgressUpdate = new ModId(-1);
        float detailsProgressTimePassed = 0f;
        float detailsProgressTimePassed_onLastTextUpdate = 0f;

#region Mod Details Panel

        internal void OpenModDetailsPanel(ModProfile profile, Action actionToInvokeWhenClosed)
        {
            ModDetailsProgressTab.Setup(profile);
            
            modDetailsOnCloseAction = actionToInvokeWhenClosed;
            GoToPanel_deprecating(ModDetailsPanel);
            SelectionManager.Instance.SelectView(UiViews.ModDetails);
            HydrateModDetailsPanel(profile);
        }

        public void CloseModDetailsPanel()
        {
            ModDetailsPanel.SetActive(false);
            modDetailsOnCloseAction?.Invoke();

            if(mouseNavigation)
            {
                SelectionOverlayHandler.Instance.SetBrowserModListItemOverlayActive(false);
            }
            else if (modDetailsOnCloseAction == null)
            {
                SelectionManager.Instance.SelectPreviousView();
            }
            else
            {
                modDetailsOnCloseAction.Invoke();
            }
        }

        void HydrateModDetailsPanel(ModProfile profile)
        {
            currentModProfileBeingViewed = profile;
            UpdateModDetailsSubscribeButtonText();
            ModDetailsGalleryLoadingAnimation.SetActive(true);
            ModDetailsGalleryImage[0].color = Color.clear;
            ModDetailsGalleryImage[1].color = Color.clear;
            ModDetailsName.text = profile.name;
            ModDetailsDescription.text = profile.description;
            ModDetailsSummary.text = profile.summary;
            ModDetailsFileSize.text = Utility.GenerateHumanReadableStringForBytes(profile.archiveFileSize);
            ModDetailsLastUpdated.text = profile.dateUpdated.ToShortDateString();
            ModDetailsReleaseDate.text = profile.dateLive.ToShortDateString();
            ModDetailsCreatedBy.text = profile.creatorUsername;
            ModDetailsUpVotes.text = Utility.GenerateHumanReadableNumber(profile.stats.ratingsPositive);
            ModDetailsDownVotes.text = Utility.GenerateHumanReadableNumber(profile.stats.ratingsNegative);
            ModDetailsSubscribers.text = Utility.GenerateHumanReadableNumber(profile.stats.subscriberTotal);

            int position = 0;
            galleryPosition = 0;
            ModDetailsGalleryImages = new Sprite[profile.galleryImages_640x360.Length + 1];
            ModDetailsGalleryImagesFailedToLoad = new bool[ModDetailsGalleryImages.Length];
            

            ListItem.HideListItems<GalleryImageButtonListItem>();

            List<DownloadReference> images = new List<DownloadReference>();

            images.Add(profile.logoImage_640x360);
            images.AddRange(profile.galleryImages_640x360);
            
            ModDetailsGalleryNavBar.SetActive(images.Count > 1);

            foreach(var downloadReference in images)
            {
                int thisPosition = position;
                position++;

                // if we have more than one image make pips for navigation
                if(images.Count > 1)
                {
                    ListItem li = ListItem.GetListItem<GalleryImageButtonListItem>(ModDetailsGalleryNavButtonPrefab, ModDetailsGalleryNavButtonParent, colorScheme);

                    // setup the delegate for the button click
                    Action transitionGalleryImage = delegate { TransitionToDifferentGalleryImage(thisPosition); };

                    li.Setup(transitionGalleryImage);
                }
                
                Action<ResultAnd<Texture2D>> imageDownloaded = r =>
                {
                    if(r.result.Succeeded())
                    {
                        ModDetailsGalleryImages[thisPosition] = Sprite.Create(r.value,
                            new Rect(Vector2.zero, new Vector2(r.value.width, r.value.height)), Vector2.zero);

                        if(thisPosition == galleryPosition)
                        {
                            ModDetailsGalleryFailedToLoadIcon.gameObject.SetActive(false);
                            ModDetailsGalleryLoadingAnimation.SetActive(false);
                            Image image = GetCurrentGalleryImageComponent();
                            image.sprite = ModDetailsGalleryImages[thisPosition];
                            image.color = Color.white;
                        }
                    }
                    else
                    {
                        ModDetailsGalleryImages[thisPosition] = null;
                        ModDetailsGalleryImagesFailedToLoad[thisPosition] = true;

                        if(thisPosition == galleryPosition)
                        {
                            ModDetailsGalleryLoadingAnimation.SetActive(false);
                            ModDetailsGalleryFailedToLoadIcon.gameObject.SetActive(true);
                            Image image = GetCurrentGalleryImageComponent();
                            image.sprite = null;
                            image.color = colorScheme.GetSchemeColor(ColorSetterType.LightGrey3);
                        }
                    }
                };

                ModIOUnity.DownloadTexture(downloadReference, imageDownloaded);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(ModDetailsGalleryNavButtonParent as RectTransform);


            LayoutRebuilder.ForceRebuildLayoutImmediate(ModDetailsName.transform.parent as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(ModDetailsDescription.transform.parent as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(ModDetailsDescription.transform.parent.transform.parent as RectTransform);
        }

        public void ModDetailsSubscribeButtonPress()
        {
            if(!isAuthenticated)
            {
                ModDetailsSubscribeButtonText.text = "Log in to Subscribe";
                SubscribeToModEvent(currentModProfileBeingViewed, UpdateModDetailsSubscribeButtonText);
            }
            else if(IsSubscribed(currentModProfileBeingViewed.id))
            {
                // This isnt actually subscribed to 'yet' but we make the UI toggle straight away
                ModDetailsSubscribeButtonText.text = "Subscribe";
                UnsubscribeFromModEvent(currentModProfileBeingViewed, UpdateModDetailsSubscribeButtonText);
            }
            else
            {
                // This isnt actually unsubscribed 'yet' but we make the UI toggle straight away
                ModDetailsSubscribeButtonText.text = "Unsubscribe";
                SubscribeToModEvent(currentModProfileBeingViewed, UpdateModDetailsSubscribeButtonText);
            }
            
            ModDetailsProgressTab.Setup(currentModProfileBeingViewed);

            LayoutRebuilder.ForceRebuildLayoutImmediate(ModDetailsSubscribeButtonText.transform.parent as RectTransform);
        }

        public void ModDetailsRatePositiveButtonPress()
        {
            if(!isAuthenticated)
            {
                OpenAuthenticationPanel();
                return;
            }
            RateModEvent(currentModProfileBeingViewed.id, ModRating.Positive);
        }

        public void ModDetailsRateNegativeButtonPress()
        {
            if(!isAuthenticated)
            {
                OpenAuthenticationPanel();
                return;
            }
            RateModEvent(currentModProfileBeingViewed.id, ModRating.Negative);
        }

        public void ModDetailsReportButtonPress()
        {
            Selectable selectionOnClose = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
            if (selectionOnClose == null)
            {
                //selectionOnClose = SelectionManager.Instance.GetSelectableForView(UiViews.ModDetails);
                //This won't work - it'll back to the previous view which will override any behaviour set up
                //What is the intended behaviour when backing from report in mod details?
                //Am I missing something?
            }
            Instance.OpenReportPanel(currentModProfileBeingViewed, selectionOnClose);
        }

        void UpdateModDetailsSubscribeButtonText()
        {
            if(!isAuthenticated)
            {
                ModDetailsSubscribeButtonText.text = "Log in to Subscribe";
            }
            else if(IsSubscribed(currentModProfileBeingViewed.id))
            {
                ModDetailsSubscribeButtonText.text = "Unsubscribe";
            }
            else
            {
                ModDetailsSubscribeButtonText.text = "Subscribe";  
            }

            ModIOUnity.IsAuthenticated((r) =>
            {
                if(!r.Succeeded())
                {
                    isAuthenticated = false;
                    ModDetailsSubscribeButtonText.text = "Log in to Subscribe";
                }
            });
        }

        /// <summary>
        /// This should get called frame by frame for an accurate progress estimate
        /// </summary>
        /// <param name="handle"></param>
        void UpdateModDetailsDownloadProgress(ProgressHandle handle)
        {
            ModDetailsProgressTab.UpdateProgress(handle);
            
            if( handle == null || handle.modId != currentModProfileBeingViewed.id || handle.Completed)
            {
                ModDetailsDownloadProgressDisplay.SetActive(false);
                return;
            }

            if (!ModDetailsDownloadProgressDisplay.activeSelf)
            {
                ModDetailsDownloadProgressDisplay.SetActive(true);
            }
            if(detailsModIdOfLastProgressUpdate != handle.modId)
            {
                detailsModIdOfLastProgressUpdate = handle.modId;
            }

            // progress bar fill amount
            ModDetailsDownloadProgressFill.fillAmount = handle.Progress;

            if(handle.OperationType == ModManagementOperationType.Install)
            {
                ModDetailsDownloadProgressRemaining.text = "Installing...";
                ModDetailsDownloadProgressCompleted.text = "";
                ModDetailsDownloadProgressSpeed.text = "";
                return;
            }
            
            // TODO At some point add a smarter average for displaying total time remaining
            // Remaining time text
            if (detailsProgressTimePassed - detailsProgressTimePassed_onLastTextUpdate >= 1f ||
                detailsProgressTimePassed_onLastTextUpdate > detailsProgressTimePassed)
            {
                float denominator = handle.Progress == 0 ? 0.01f : handle.Progress;
                float timeRemainingInSeconds = (detailsProgressTimePassed / denominator) - detailsProgressTimePassed;
                ModDetailsDownloadProgressRemaining.text = $"{Utility.GenerateHumanReadableTimeStringFromSeconds((int)timeRemainingInSeconds)} remaining";
                
                ModDetailsDownloadProgressSpeed.text = $"{Utility.GenerateHumanReadableStringForBytes(handle.BytesPerSecond)}/s";

                if(GetSubscribedProfile(handle.modId, out ModProfile profile))
                {
                    ModDetailsDownloadProgressCompleted.text = $"{Utility.GenerateHumanReadableStringForBytes((long)(profile.archiveFileSize * handle.Progress))} of {Utility.GenerateHumanReadableStringForBytes(profile.archiveFileSize)}";
                }
                else
                {
                    ModDetailsDownloadProgressCompleted.text = "--";
                }

                detailsProgressTimePassed_onLastTextUpdate = detailsProgressTimePassed;
            }
            detailsProgressTimePassed += Time.deltaTime;
        }

        internal void ShowNextGalleryImage()
        {
            int index = Utility.GetNextIndex(galleryPosition, ModDetailsGalleryImages.Length);
            TransitionToDifferentGalleryImage(index);
        }

        internal void ShowPreviousGalleryImage()
        {
            int index = Utility.GetPreviousIndex(galleryPosition, ModDetailsGalleryImages.Length);
            TransitionToDifferentGalleryImage(index);
        }

        void TransitionToDifferentGalleryImage(int index)
        {
            if(galleryTransition != null)
            {
                StopCoroutine(galleryTransition);
            }
            galleryTransition = TransitionGalleryImage(index);
            StartCoroutine(galleryTransition);
        }

        IEnumerator TransitionGalleryImage(int index)
        {
            galleryPosition = index;
            if(index >= ModDetailsGalleryImages.Length)
            {
                // It's likely we haven't loaded the gallery images yet
                yield break;
            }

            Image next = GetNextGalleryImageComponent();
            Image current = GetCurrentGalleryImageComponent();

            if(current.sprite == ModDetailsGalleryImages[index])
            {
                // Stop the transition, we are already showing the gallery image we want to transition to
                yield break;
            }

            galleryImageInUse = !galleryImageInUse;

            next.sprite = ModDetailsGalleryImages[index];
            if(next.sprite == null)
            {
                ModDetailsGalleryFailedToLoadIcon.gameObject.SetActive(true);
                next.color = colorScheme.GetSchemeColor(ColorSetterType.LightGrey3);
            }
            else
            {
                ModDetailsGalleryFailedToLoadIcon.gameObject.SetActive(false);
                next.color = Color.white;
            }

            float time;
            float timePassed = 0f;
            Color colIn = next.color;
            Color colFailedIcon = ModDetailsGalleryFailedToLoadIcon.color;
            Color colOut = current.color;
            colIn.a = 0f;
            colFailedIcon.a = 0f;

            while(timePassed <= galleryTransitionTime)
            {
                time = timePassed / galleryTransitionTime;

                colIn.a = time;
                colFailedIcon.a = time;
                colOut.a = 1f - time;

                next.color = colIn;
                ModDetailsGalleryFailedToLoadIcon.color = colFailedIcon;
                current.color = colOut;

                yield return null;
                timePassed += Time.deltaTime;
            }
        }

        Image GetCurrentGalleryImageComponent()
        {
            int current = galleryImageInUse ? 0 : 1;
            return ModDetailsGalleryImage[current];
        }

        Image GetNextGalleryImageComponent()
        {
            int next = galleryImageInUse ? 1 : 0;
            return ModDetailsGalleryImage[next];
        }

#endregion

    }
}
