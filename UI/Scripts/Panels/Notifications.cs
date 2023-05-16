using System.Collections;
using System.Collections.Generic;
using ModIO;
using ModIO.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    class Notifications : SelfInstancingMonoSingleton<Notifications>
    {
        [Header("Notifications")]
        [SerializeField] GameObject NotificationPanel;
        [SerializeField] Image NotificationPanelImage;
        [SerializeField] Image NotificationPanelIconBackgroundImage;
        [SerializeField] Image NotificationPanelIconImage;
        [SerializeField] TMP_Text NotificationPanelTitle;
        [SerializeField] TMP_Text NotificationPanelDescription;
        [SerializeField] Sprite NotificationErrorIcon;
        [SerializeField] Sprite NotificationCheckmarkIcon;

        public class QueuedNotice
        {
            public string title;
            public string description;
            public bool positiveAccent;
        }

        Queue<QueuedNotice> upcomingNotices = new Queue<QueuedNotice>();
        bool showingNotice = false;
        Vector2 notificationOrigin = new Vector2(24f, 24f);

        #region Monobehaviour
        // Because of the coroutine, disabling this browser will also cancel the coroutine, make sure
        // we have turned this notice off and cleared the notification queue
        void OnDisable()
        {
            NotificationPanel.SetActive(false);
            upcomingNotices.Clear();
            showingNotice = false;
        }
        #endregion // Monobehaviour

        #region Notification Panel

        public void ProcessModManagementEventIntoNotification(ModManagementEventType type, ModId modId, Result result)
        {
            switch(type)
            {
                case ModManagementEventType.Installed:
                    AddNotificationToQueue(new QueuedNotice
                    {
                        title = TranslationManager.Instance.Get("Mod installed"),
                        description = TranslationManager.Instance.Get("{modname} has finished installing", $"{Collection.Instance.GetModNameFromId(modId)}"),
                        positiveAccent = true
                    });
                    break;
                case ModManagementEventType.InstallFailed:

                    string description = result.IsStorageSpaceInsufficient() ?
                        TranslationManager.Instance.Get("Not enough space") :
                        TranslationManager.Instance.Get("{modname} failed to install", $"{Collection.Instance.GetModNameFromId(modId)}");

                    AddNotificationToQueue(new QueuedNotice
                    {
                        title = TranslationManager.Instance.Get("Mod installation failed"),
                        description = description,
                        positiveAccent = false
                    });
                    break;
                case ModManagementEventType.DownloadFailed:

                    description = result.IsStorageSpaceInsufficient() ?
                        TranslationManager.Instance.Get("Not enough space") :
                        TranslationManager.Instance.Get("{modname} failed to download", $"{Collection.Instance.GetModNameFromId(modId)}");

                    AddNotificationToQueue(new QueuedNotice
                    {
                        title = TranslationManager.Instance.Get("Mod download failed"),
                        description = description,
                        positiveAccent = false
                    });
                    break;
                case ModManagementEventType.UninstallFailed:
                    AddNotificationToQueue(new QueuedNotice
                    {
                        title = TranslationManager.Instance.Get("Mod delete failed"),
                        description = TranslationManager.Instance.Get("{modname} failed to delete", $"{Collection.Instance.GetModNameFromId(modId)}"),

                        positiveAccent = false
                    });
                    break;
                case ModManagementEventType.Updated:
                    AddNotificationToQueue(new QueuedNotice
                    {
                        title = TranslationManager.Instance.Get("Mod updated"),
                        description = TranslationManager.Instance.Get("{modname} has finished updating", $"{Collection.Instance.GetModNameFromId(modId)}"),

                        positiveAccent = true
                    });
                    break;
                case ModManagementEventType.UpdateFailed:
                    AddNotificationToQueue(new QueuedNotice
                    {
                        title = TranslationManager.Instance.Get("Mod update failed"),
                        description = TranslationManager.Instance.Get("{modname} failed to update", $"{Collection.Instance.GetModNameFromId(modId)}"),

                        positiveAccent = false
                    });
                    break;
                default:
                    return;
            }
        }

        public void AddNotificationToQueue(QueuedNotice notice)
        {
            // Cap the number of notices at 5 so we dont get flooding or outdated notices
            if(upcomingNotices.Count > 5)
            {
                return;
            }

            // Add notice to the queue
            upcomingNotices.Enqueue(notice);

            // Attempt to show next notice (ignored if already in transition or none to show)
            StartCoroutine(ShowNextNotice());
        }

        IEnumerator ShowNextNotice()
        {
            if(showingNotice || upcomingNotices.Count == 0)
            {
                yield break;
            }

            showingNotice = true;
            QueuedNotice notice = upcomingNotices.Dequeue();
            NotificationPanelTitle.text = notice.title;
            NotificationPanelDescription.text = notice.description;
            NotificationPanelIconBackgroundImage.color = notice.positiveAccent ?
                Browser.Instance.colorScheme.PositiveAccent : SharedUi.colorScheme.NegativeAccent;
            NotificationPanelIconImage.sprite = notice.positiveAccent ?
                NotificationCheckmarkIcon : NotificationErrorIcon;

            // get graphics to fade
            List<Graphic> graphics = new List<Graphic>
            {
                NotificationPanelImage,
                NotificationPanelIconImage,
                NotificationPanelIconBackgroundImage,
                NotificationPanelTitle,
                NotificationPanelDescription
            };

            //set graphics to alpha == 0
            foreach(Graphic graphic in graphics)
            {
                Color color = graphic.color;
                color.a = 0;
                graphic.color = color;
            }

            // setup transition variables
            int totalIncrements = 10;
            float alphaChangePerIncrement = 1f / totalIncrements;
            float timeBetweenIncrements = 0.02f;
            float totalVerticalMovement = 32f;
            float verticalMovementPerIncrement = totalVerticalMovement / totalIncrements;

            // activate the panel and position it
            Vector2 originOffset = notificationOrigin;
            originOffset.y -= totalVerticalMovement;
            NotificationPanel.transform.position = originOffset;
            NotificationPanel.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(NotificationPanel.transform as RectTransform);

            // do transition
            for(int i = 0; i < totalIncrements; i++)
            {
                Vector2 pos = NotificationPanel.transform.position;
                pos.y += verticalMovementPerIncrement;
                NotificationPanel.transform.position = pos;

                foreach(Graphic graphic in graphics)
                {
                    Color color = graphic.color;
                    color.a += alphaChangePerIncrement;
                    graphic.color = color;
                }
                yield return new WaitForSecondsRealtime(timeBetweenIncrements);
            }

            // wait for 4 seconds
            yield return new WaitForSecondsRealtime(3f);

            // transition out
            for(int i = 0; i < totalIncrements; i++)
            {
                foreach(Graphic graphic in graphics)
                {
                    Color color = graphic.color;
                    color.a -= alphaChangePerIncrement;
                    graphic.color = color;
                }
                yield return new WaitForSecondsRealtime(timeBetweenIncrements);
            }

            NotificationPanel.SetActive(false);
            showingNotice = false;

            // Attempt to show next notice (ignored if already in transition or none to show)
            StartCoroutine(ShowNextNotice());
        }

        #endregion //  Notification Panel

    }
}
