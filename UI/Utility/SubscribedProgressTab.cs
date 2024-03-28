using System;
using System.Collections.Generic;
using ModIO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ModIOBrowser.Implementation;
using System.Collections.Specialized;

namespace ModIOBrowser
{
	public class SubscribedProgressTab : MonoBehaviour
	{
		public GameObject progressBar;
		public Image progressBarFill;
		public TMP_Text progressBarText;
		public GameObject progressBarQueuedOutline;
		public ModProfile profile;

#pragma warning disable 0649 //it is allocated
        Translation progressBarTextTranslation;
#pragma warning restore 0649

		static List<SubscribedProgressTab> allProgressTabs = new List<SubscribedProgressTab>();

		void Awake()
		{
			allProgressTabs.Add(this);
		}

		public static void UpdateProgressTab(ModManagementEventType eventType, ModId id)
		{
			foreach(var tab in allProgressTabs)
			{
				if(tab.profile.id == id)
				{
					tab.UpdateStatus(eventType, id);
				}
			}
		}

		public static void HideAllTabs()
		{
			allProgressTabs.ForEach(x=>x.Hide());
		}

		public void Setup(ModProfile profile)
		{
			this.profile = profile;
				
			if(Collection.Instance.IsSubscribed(profile.id, out SubscribedModStatus status))
			{
				if(status == SubscribedModStatus.Installed)
				{
                    Translation.Get(progressBarTextTranslation, "Subscribed", progressBarText);
					progressBarFill.fillAmount = 1f;
					progressBarQueuedOutline.SetActive(false);
				}
				else
				{
                    Translation.Get(progressBarTextTranslation, "Queued", progressBarText);
					progressBarFill.fillAmount = 0f;
					progressBarQueuedOutline.SetActive(true);
				}
				progressBar.SetActive(true);
			}
			else
			{
				progressBar.SetActive(false);
				progressBarQueuedOutline.SetActive(false);
			}
		}

		public void MimicOtherProgressTab(SubscribedProgressTab other)
		{
			if (other == null)
                Debug.LogWarning("Other is null");
			if (progressBar == null)
                Debug.LogWarning("progressBar is null");

			progressBar.SetActive(other.progressBar.activeSelf);
			progressBarFill.fillAmount = other.progressBarFill.fillAmount;
			progressBarText.text = other.progressBarText.text;
			progressBarQueuedOutline.SetActive(other.progressBarQueuedOutline.activeSelf);
		}

		public void UpdateProgress(ProgressHandle handle)
		{
			if(handle == null || handle.modId != profile.id)
			{
				return;
			}
			
			progressBarQueuedOutline.SetActive(false);
			
			progressBar.SetActive(Collection.Instance.IsSubscribed(profile.id));

			progressBarFill.fillAmount = handle.Progress;
            
			switch(handle.OperationType)
			{
				case ModManagementOperationType.None_AlreadyInstalled:
					progressBar.SetActive(true);
                    Translation.Get(progressBarTextTranslation, "Subscribed", progressBarText);
                    break;
				case ModManagementOperationType.None_ErrorOcurred:
					break;
				case ModManagementOperationType.Install:
					progressBar.SetActive(true);					
                    Translation.Get(progressBarTextTranslation, "Installing", progressBarText);
                    break;
				case ModManagementOperationType.Download:
					progressBar.SetActive(true);
                    Translation.Get(progressBarTextTranslation, "Downloading", progressBarText);
                    break;
				case ModManagementOperationType.Uninstall:
					break;
				case ModManagementOperationType.Update:
					progressBar.SetActive(true);
                    Translation.Get(progressBarTextTranslation, "Updating", progressBarText);
                    break;
			}
		}
		
		internal void Hide()
		{
			progressBar.SetActive(false);
		}
		
		internal void UpdateStatus(ModManagementEventType updatedStatus, ModId id)
        {
	        if(profile.id != id)
	        {
		        return;
	        }

	        progressBar.SetActive(Collection.Instance.IsSubscribed(id));

	        // Always turn this off when state changes. It will auto get turned back on if needed
            progressBar.SetActive(false);
            progressBarQueuedOutline.SetActive(false);

            switch(updatedStatus)
            {
	            case ModManagementEventType.UpdateFailed:
	            case ModManagementEventType.InstallFailed:
	            case ModManagementEventType.DownloadFailed:
	            case ModManagementEventType.UninstallStarted:
	            case ModManagementEventType.Uninstalled:
	            case ModManagementEventType.UninstallFailed:
                    Translation.Get(progressBarTextTranslation, "Error", progressBarText);
		            progressBarFill.fillAmount = 0f;
		            break;
                case ModManagementEventType.InstallStarted:
                    Translation.Get(progressBarTextTranslation, "Installing", progressBarText);                    
                    progressBarFill.fillAmount = 1f;
                    progressBar.SetActive(true);
                    break;
                case ModManagementEventType.Installed:
                    Translation.Get(progressBarTextTranslation, "Subscribed", progressBarText);
                    progressBarFill.fillAmount = 1f;
                    progressBar.SetActive(true);
                    break;
                case ModManagementEventType.DownloadStarted:
                    Translation.Get(progressBarTextTranslation, "Downloading", progressBarText);
                    progressBar.SetActive(true);
                    break;
                case ModManagementEventType.Downloaded:
                    Translation.Get(progressBarTextTranslation, "Downloaded", progressBarText);
                    progressBarFill.fillAmount = 1f;
                    progressBar.SetActive(true);
                    break;
                case ModManagementEventType.UpdateStarted:
                    Translation.Get(progressBarTextTranslation, "Updating", progressBarText);
                    progressBar.SetActive(true);
                    break;
                case ModManagementEventType.Updated:
                    Translation.Get(progressBarTextTranslation, "Subscribed", progressBarText);
                    progressBarFill.fillAmount = 1f;
                    progressBar.SetActive(true);
                    break;
            }
        }
	}
}
