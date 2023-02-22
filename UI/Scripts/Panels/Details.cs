using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ModIO;
using UnityEngine.EventSystems;
using ModIO.Util;

namespace ModIOBrowser.Implementation
{
    public class Details : SimpleMonoSingleton<Details>
    {
        
        [Header("Mod Details Panel")]
        [SerializeField] public GameObject ModDetailsPanel;
        [SerializeField]
        public RectTransform ModDetailsContentRect;
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
        [SerializeField]
        public SubscribedProgressTab ModDetailsProgressTab;
        [SerializeField]
        public GameObject ModDetailsScrollToggleGameObject;
        bool galleryImageInUse;
        Sprite[] ModDetailsGalleryImages;
        bool[] ModDetailsGalleryImagesFailedToLoad;
        int galleryPosition;
        float galleryTransitionTime = 0.3f;
        IEnumerator galleryTransition;
        ModProfile currentModProfileBeingViewed;
        IEnumerator downloadProgressUpdater;

        internal Translation ModDetailsSubscribeButtonTextTranslation = null;
        
        //cached nav button list items
        List<ListItem> _listItems = new List<ListItem>();

        //The navigation button index that represents the position in the image gallery
        int activateNavButtonIndex = 0;

        Coroutine _autoRotateImagesCoroutine;

        // We set this action from where the panel is opened so we can also set which panel to re-open when this details panel is closed
        Action modDetailsOnCloseAction;

        // measuring the progress bar
        ModId detailsModIdOfLastProgressUpdate = new ModId(-1);
        float detailsProgressTimePassed = 0f;
        float detailsProgressTimePassed_onLastTextUpdate = 0f;

        public static bool IsOn() => Instance != null && Instance.ModDetailsPanel != null && Instance.ModDetailsPanel.activeSelf;
        
        internal void Open(ModProfile profile, Action actionToInvokeWhenClosed)
        {
            ModDetailsProgressTab.Setup(profile);

            modDetailsOnCloseAction = actionToInvokeWhenClosed;
            Navigating.GoToPanel(ModDetailsPanel);
            SelectionManager.Instance.SelectView(UiViews.ModDetails);
            Refresh(profile);
            _autoRotateImagesCoroutine = StartCoroutine(AutoRotateImages());
        }

        public void Close()
        {
            ModDetailsPanel.SetActive(false);
            modDetailsOnCloseAction?.Invoke();

            ListItemsCleanup();
            StopCoroutine(_autoRotateImagesCoroutine);

            if(InputNavigation.Instance.mouseNavigation)
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

        void Refresh(ModProfile profile)
        {
            currentModProfileBeingViewed = profile;
            UpdateSubscribeButtonText();
            ModDetailsGalleryLoadingAnimation.SetActive(true);
            ModDetailsGalleryImage[0].color = Color.clear;
            ModDetailsGalleryImage[1].color = Color.clear;
            ModDetailsName.text = profile.name;
            ModDetailsDescription.text = profile.description;
            ModDetailsSummary.text = profile.summary;
            ModDetailsFileSize.text = Utility.GenerateHumanReadableStringForBytes(profile.archiveFileSize);
            ModDetailsLastUpdated.text = TranslationManager.Instance.SelectedLanguage.DateShort(profile.dateUpdated);
            ModDetailsReleaseDate.text = TranslationManager.Instance.SelectedLanguage.DateShort(profile.dateLive);
            ModDetailsCreatedBy.text = profile.creator.username;
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
                    ListItem li = ListItem.GetListItem<GalleryImageButtonListItem>(ModDetailsGalleryNavButtonPrefab, ModDetailsGalleryNavButtonParent, Browser.Instance.colorScheme);
                    li.Setup(delegate { this.OnNavButtonClicked(thisPosition); });
                    _listItems.Add(li);
                }

                var scopePosition = thisPosition;
                Action<ResultAnd<Texture2D>> imageDownloaded = r =>
                {
                    if(r.result.Succeeded())
                    {                        
                        QueueRunner.Instance.AddSpriteCreation(r.value, sprite =>
                        {
                            ModDetailsGalleryImages[scopePosition] = sprite;
                            if(scopePosition == galleryPosition)
                            {
                                ModDetailsGalleryFailedToLoadIcon.gameObject.SetActive(false);
                                ModDetailsGalleryLoadingAnimation.SetActive(false);
                                Image image = GetCurrentGalleryImageComponent();
                                image.sprite = ModDetailsGalleryImages[scopePosition];
                                image.color = Color.white;
                            }
                        });

                        //ModDetailsGalleryImages[thisPosition] = Sprite.Create(r.value,
                        //    new Rect(Vector2.zero, new Vector2(r.value.width, r.value.height)), Vector2.zero);

                        //if(thisPosition == galleryPosition)
                        //{
                        //    ModDetailsGalleryFailedToLoadIcon.gameObject.SetActive(false);
                        //    ModDetailsGalleryLoadingAnimation.SetActive(false);
                        //    Image image = GetCurrentGalleryImageComponent();
                        //    image.sprite = ModDetailsGalleryImages[thisPosition];
                        //    image.color = Color.white;
                        //}
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
                            image.color = Browser.Instance.colorScheme.GetSchemeColor(ColorSetterType.Inactive3);
                        }
                    }
                };

                ModIOUnity.DownloadTexture(downloadReference, imageDownloaded);
            }
            ActivateButton(0);

            LayoutRebuilder.ForceRebuildLayoutImmediate(ModDetailsGalleryNavButtonParent as RectTransform);


            LayoutRebuilder.ForceRebuildLayoutImmediate(ModDetailsName.transform.parent as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(ModDetailsDescription.transform.parent as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(ModDetailsDescription.transform.parent.transform.parent as RectTransform);
        }

        public void SubscribeButtonPress()
        {
            if(!Authentication.Instance.IsAuthenticated)
            {
                Translation.Get(ModDetailsSubscribeButtonTextTranslation, "Log in to Subscribe", ModDetailsSubscribeButtonText);
                Mods.SubscribeToEvent(currentModProfileBeingViewed, UpdateSubscribeButtonText);
            }
            else if(Collection.Instance.IsSubscribed(currentModProfileBeingViewed.id))
            {
                // This isnt actually subscribed to 'yet' but we make the UI toggle straight away
                Translation.Get(ModDetailsSubscribeButtonTextTranslation, "Subscribe", ModDetailsSubscribeButtonText);
                Mods.UnsubscribeFromEvent(currentModProfileBeingViewed, UpdateSubscribeButtonText);
            }
            else
            {
                Translation.Get(ModDetailsSubscribeButtonTextTranslation, "Unsubscribe", ModDetailsSubscribeButtonText);
                Mods.SubscribeToEvent(currentModProfileBeingViewed, UpdateSubscribeButtonText);
            }

            ModDetailsProgressTab.Setup(currentModProfileBeingViewed);

            LayoutRebuilder.ForceRebuildLayoutImmediate(ModDetailsSubscribeButtonText.transform.parent as RectTransform);
        }

        public void RatePositiveButtonPress()
        {
            if(!Authentication.Instance.IsAuthenticated)
            {
                AuthenticationPanels.Instance.Open();
                return;
            }
            Mods.RateEvent(currentModProfileBeingViewed.id, ModRating.Positive);
        }

        public void RateNegativeButtonPress()
        {
            if(!Authentication.Instance.IsAuthenticated)
            {
                AuthenticationPanels.Instance.Open();
                return;
            }
            Mods.RateEvent(currentModProfileBeingViewed.id, ModRating.Negative);
        }

        public void ReportButtonPress()
        {
            Selectable selectionOnClose = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
            if (selectionOnClose == null)
            {
                //selectionOnClose = SelectionManager.Instance.GetSelectableForView(UiViews.ModDetails);
                //This won't work - it'll back to the previous view which will override any behaviour set up
                //What is the intended behaviour when backing from report in mod details?
                //Am I missing something?
            }
            Reporting.Instance.Open(currentModProfileBeingViewed, selectionOnClose);
        }

        public void UpdateSubscribeButtonText()
        {
            if(!Authentication.Instance.IsAuthenticated)
            {
                Translation.Get(ModDetailsSubscribeButtonTextTranslation, "Log in to Subscribe", ModDetailsSubscribeButtonText);
            }
            else if(Collection.Instance.IsSubscribed(currentModProfileBeingViewed.id))
            {
                Translation.Get(ModDetailsSubscribeButtonTextTranslation, "Unsubscribe", ModDetailsSubscribeButtonText);
            }
            else
            {
                Translation.Get(ModDetailsSubscribeButtonTextTranslation, "Subscribe", ModDetailsSubscribeButtonText);
            }

            ModIOUnity.IsAuthenticated((r) =>
            {
                if(!r.Succeeded())
                {
                    Authentication.Instance.IsAuthenticated = false;
                    Translation.Get(ModDetailsSubscribeButtonTextTranslation, "Log in to Subscribe", ModDetailsSubscribeButtonText);
                }
            });
        }

        /// <summary>
        /// This should get called frame by frame for an accurate progress estimate
        /// </summary>
        /// <param name="handle"></param>
        public void UpdateDownloadProgress(ProgressHandle handle)
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
                ModDetailsDownloadProgressRemaining.text = TranslationManager.Instance.Get("Installing...");
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

                ModDetailsDownloadProgressRemaining.text = TranslationManager.Instance.Get("{seconds} remaining", $"{ Utility.GenerateHumanReadableTimeStringFromSeconds((int)timeRemainingInSeconds)}");
                ModDetailsDownloadProgressSpeed.text = TranslationManager.Instance.Get("{BytesPerSecond)}/s", Utility.GenerateHumanReadableStringForBytes(handle.BytesPerSecond));

                if(Collection.Instance.GetSubscribedProfile(handle.modId, out ModProfile profile))
                {
                    TranslationManager.Instance.Get("{A} of {B}",
                        $"{ Utility.GenerateHumanReadableStringForBytes((long)(profile.archiveFileSize * handle.Progress))}",
                        $"{ Utility.GenerateHumanReadableStringForBytes(profile.archiveFileSize)}");
                }
                else
                {
                    ModDetailsDownloadProgressCompleted.text = "--";
                }

                detailsProgressTimePassed_onLastTextUpdate = detailsProgressTimePassed;
            }
            detailsProgressTimePassed += Time.deltaTime;
        }

        internal void GalleryImageTransition(bool showNext)
        {
            StopCoroutine(this._autoRotateImagesCoroutine);
            if(showNext)
            {
                ShowNextGalleryImage();
            }
            else
            {
                ShowPreviousGalleryImage();
            }
        }

        public void ShowNextGalleryImage()
        {
            int index = GetNextIndex(galleryPosition, ModDetailsGalleryImages.Length);
            TransitionToDifferentGalleryImage(index);
            ActivateButton(index);
        }

        public void ShowPreviousGalleryImage()
        {
            int index = GetPreviousIndex(galleryPosition, ModDetailsGalleryImages.Length);
            TransitionToDifferentGalleryImage(index);
            ActivateButton(index);
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
                next.color = Browser.Instance.colorScheme.GetSchemeColor(ColorSetterType.Inactive3);
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

        //Selects a navigation button and deselects the previously selected button
        void ActivateButton(int toggledIndex)
        {
            if(toggledIndex < _listItems.Count)
            {
                _listItems[activateNavButtonIndex].DeSelect();
                _listItems[toggledIndex].Select();
                activateNavButtonIndex = toggledIndex;
            }
        }

        void ListItemsCleanup()
        {
            if(_listItems.Count > activateNavButtonIndex)
                _listItems[activateNavButtonIndex].DeSelect();
            activateNavButtonIndex = 0;
            this._listItems.Clear();
        }

        IEnumerator AutoRotateImages()
        {
            while(true)
            {
                yield return new WaitForSecondsRealtime(3);
                this.ShowNextGalleryImage();
            }

            // ReSharper disable once IteratorNeverReturns
        }

        void OnNavButtonClicked(int position)
        {
            TransitionToDifferentGalleryImage(position);
            ActivateButton(position);
            StopCoroutine(this._autoRotateImagesCoroutine);
        }

        #region Utility

        public static int GetPreviousIndex(int current, int length)
        {
            if(length == 0)
            {
                return 0;
            }

            current -= 1;
            if(current < 0)
            {
                current = length - 1;
            }
            return current;
        }

        public static int GetNextIndex(int current, int length)
        {
            if(length == 0)
            {
                return 0;
            }

            current += 1;
            if(current >= length)
            {
                current = 0;
            }
            return current;
        }

        #endregion
    }
}
