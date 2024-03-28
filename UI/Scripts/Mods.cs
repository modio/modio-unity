using System;
using ModIO;
using ModIO.Implementation.API.Objects;
using ModIOBrowser.Implementation;

namespace ModIOBrowser
{
    static class Mods
    {
        // edge case solve for pressing rate mod repeatedly
        static ModId lastRatedMod;
        static ModRating lastRatingType;

        // globally cached and used to keep track of the current mod management operation progress
        public static ProgressHandle CurrentModManagementOperationHandle;

        public static ModManagementEventDelegate OnModManagementEvent;

        /// <summary>
        /// This works the same as UnsubscribeFromModEvent() but it subscribes instead.
        /// Always use this method to subscribe to mods so that we can globally track the result and
        /// make updates where necessary.
        /// Eg update the mod collection list or 'subscribed' pips on mod list items displayed in
        /// the home view.
        /// </summary>
        /// <param name="profile">the mod being subscribed to</param>
        /// <param name="callback">any extra callback to run once the response is received</param>
        internal static void SubscribeToEvent(ModProfile profile, Action callback = null)
        {
            if(!Authentication.Instance.IsAuthenticated)
            {
                AuthenticationPanels.Instance?.Open();
                return;
            }

            Collection.Instance.pendingSubscriptions.Add(profile);

            ModIOUnity.SubscribeToMod(profile.id,
                delegate (Result result)
                {
                    Collection.Instance.pendingSubscriptions.Remove(profile);

                    if(result.Succeeded())
                    {
                        Notifications.Instance.AddNotificationToQueue(new Notifications.QueuedNotice
                        {
                            title = TranslationManager.Instance.Get("Subscribed"),
                            description = TranslationManager.Instance.Get("{GetModNameFromId} has been added to the download queue", Collection.Instance.GetModNameFromId(profile.id)),
                            positiveAccent = true
                        });

                        Collection.Instance.CacheLocalSubscribedModStatuses();

                        // if collection open, make another list item for the new subscribed item
                        if(Collection.IsOn())
                        {
                            Collection.Instance.RefreshList();
                        }
                    }
                    else
                    {
                        Notifications.Instance.AddNotificationToQueue(new Notifications.QueuedNotice
                        {
                            title = TranslationManager.Instance.Get("Failed to subscribe"),
                            description = TranslationManager.Instance.Get("Unable to subscribe to '{GetModNameFromId}'", Collection.Instance.GetModNameFromId(profile.id)),
                            positiveAccent = false
                        });
                    }

                    callback?.Invoke();
                });
        }


        /// <summary>
        /// This works the same as SubscribeToModEvent() but it unsubscribes instead.
        /// Always use this method to unsubscribe from mods so that we can globally track the result and
        /// make updates where necessary.
        /// Eg update the mod collection list or 'subscribed' pips on mod list items displayed in
        /// the home view.
        /// </summary>
        /// <param name="profile">the mod to unsubscribe</param>
        /// <param name="callback">any extra callback to run once the response is received</param>
        public static void UnsubscribeFromEvent(ModProfile profile, Action callback = null)
        {
            if(!Authentication.Instance.IsAuthenticated)
            {
                return;
            }

            Collection.Instance.pendingSubscriptions.Remove(profile);
            if(!Collection.Instance.pendingUnsubscribes.Contains(profile.id))
            {
                Collection.Instance.pendingUnsubscribes.Add(profile.id);
            }

            ModIOUnity.UnsubscribeFromMod(profile.id,
                delegate (Result result)
                {
                    if(Collection.Instance.pendingUnsubscribes.Contains(profile.id))
                    {
                        Collection.Instance.pendingUnsubscribes.Remove(profile.id);
                    }
                    if(result.Succeeded())
                    {
                        Notifications.Instance.AddNotificationToQueue(new Notifications.QueuedNotice
                        {
                            title = TranslationManager.Instance.Get("Unsubscribed"),
                            description = TranslationManager.Instance.Get("{GetNameFromModId} has been removed from your collection", Collection.Instance.GetModNameFromId(profile.id)),
                            positiveAccent = true
                        });

                        Collection.Instance.CacheLocalSubscribedModStatuses();
                    }

                    callback?.Invoke();
                });
        }

        /// <summary>
        /// Always use this method to when we want to rate a mod so that when we receive a result we
        /// can update any UI accordingly.
        /// Eg update the mod details Rate up/down buttons to show that you've rated this mod
        /// </summary>
        /// <param name="modId">the mod id to rate</param>
        /// <param name="rating">the rating to apply, eg ModRating.Positive</param>
        /// <param name="callback">any extra callback to run once the response is received</param>
        public static void RateEvent(ModId modId, ModRating rating, Action callback = null)
        {
            if(!Authentication.Instance.IsAuthenticated)
            {
                return;
            }

            ModIOUnity.RateMod(modId, rating, delegate (Result result)
            {
                callback?.Invoke();

                if(result.Succeeded())
                {
                    // make sure we arent repeatedly sending the same rating
                    if(lastRatedMod != modId || lastRatingType != rating)
                    {
                        lastRatingType = rating;
                        lastRatedMod = modId;

                        Notifications.Instance.AddNotificationToQueue(new Notifications.QueuedNotice
                        {
                            title = TranslationManager.Instance.Get("Rating added"),
                            description = TranslationManager.Instance.Get("Your rating has been added for {Mod}", Collection.Instance.GetModNameFromId(modId)),
                            positiveAccent = true
                        });
                    }
                }
                else
                {
                    Details.Instance.UpdateRatingButtons();
                    Notifications.Instance.AddNotificationToQueue(new Notifications.QueuedNotice
                    {
                        title = TranslationManager.Instance.Get("Failed to add rating"),
                        description = TranslationManager.Instance.Get("Failed to submit your rating for {Mod}", Collection.Instance.GetModNameFromId(modId)),
                        positiveAccent = false
                    });
                }
            });
        }


        #region ModManagement Operation
        /*
         * This region contains the methods used for handling the ModManagementEventDelegate
         * as well as the ProgressHandle given from GetCurrentModManagementOperation()
         */

        /// <summary>
        /// This is assigned when the browser is initialized and EnableModManagement is invoked
        /// </summary>
        /// <param name="type">the type of MM event</param>
        /// <param name="id">the mod id pertaining to this event</param>
        public static void ModManagementEvent(ModManagementEventType type, ModId id, Result eventResult)
        {
            // invoke any external delegates provided to also listen for these events
            OnModManagementEvent?.Invoke(type, id, eventResult);

            // cache not enough storage mods
            if(eventResult.IsStorageSpaceInsufficient())
            {
                if(!Collection.Instance.notEnoughSpaceForTheseMods.Contains(id))
                {
                    Collection.Instance.notEnoughSpaceForTheseMods.Add(id);
                }
            }

            if(!Browser.IsOpen)
            {
                return;
            }
            Notifications.Instance.ProcessModManagementEventIntoNotification(type, id, eventResult);

            //maybe keep this in here instead?
            CurrentModManagementOperationHandle = ModIOUnity.GetCurrentModManagementOperation();
            if(CurrentModManagementOperationHandle.Completed)
            {
                CurrentModManagementOperationHandle = null;
            }

            Collection.Instance.CacheLocalSubscribedModStatuses();

            if(CollectionModListItem.listItems.ContainsKey(id))
            {
                CollectionModListItem.listItems[id].UpdateStatus(type);
            }
            if(HomeModListItem.listItems.ContainsKey(id))
            {
                HomeModListItem.listItems[id].UpdateStatus(type, id);
            }
            if(Details.IsOn())
            {
                Details.Instance.ModDetailsProgressTab.UpdateStatus(type, id);
            }

            Home.ModManagementEvent(type, id, eventResult);

            if(DownloadQueue.Instance.DownloadQueuePanel.activeSelf)
            {
                DownloadQueue.Instance.RefreshDownloadHistoryPanel();
            }
        }

        /// <summary>
        /// This looks for progress bars across the UI and invokes an update if needed, using the
        /// provided ProgressHandle.Progress field to do so.
        /// </summary>
        /// <param name="handle">this can be null and will be handled appropriately</param>
        internal static void UpdateProgressState() => UpdateProgressStateInternal(CurrentModManagementOperationHandle);

        private static void UpdateProgressStateInternal(ProgressHandle handle)
        {
            if(handle == null)
            {
                CurrentModManagementOperationHandle = ModIOUnity.GetCurrentModManagementOperation();
            }

            Avatar.Instance.UpdateDownloadProgressBar(handle);

            if(Collection.IsOn())
            {
                if(handle != null && CollectionModListItem.listItems.ContainsKey(handle.modId))
                {
                    CollectionModListItem.listItems[handle.modId].UpdateProgressState(handle);
                }
            }
            else if(Details.IsOn())
            {
                Details.Instance.UpdateDownloadProgress(handle);
            }
            else if(handle != null && SearchResults.Instance.SearchResultsPanel.activeSelf)
            {
                if(SearchResultListItem.listItems.ContainsKey(handle.modId))
                {
                    SearchResultListItem.listItems[handle.modId].UpdateProgressBar(handle);
                }
            }
            Home.UpdateProgressState(handle);

            // Check this separately because it's an overlay
            if(DownloadQueue.Instance.DownloadQueuePanel.activeSelf)
            {
                DownloadQueue.Instance.UpdateDownloadQueueCurrentDownloadDisplay(handle);
            }
        }


        #endregion
    }
}
