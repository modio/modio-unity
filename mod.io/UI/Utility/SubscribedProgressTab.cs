using ModIO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace ModIOBrowser
{
	public class SubscribedProgressTab : MonoBehaviour
	{
		public GameObject progressBar;
		public Image progressBarFill;
		public TMP_Text progressBarText;
		public GameObject progressBarQueuedOutline;
		public ModProfile profile;

		public void Setup(ModProfile profile)
		{
			this.profile = profile;
				
			if(Browser.IsSubscribed(profile.id, out SubscribedModStatus status))
			{
				if(status == SubscribedModStatus.Installed)
				{
					progressBarText.text = "Subscribed";
					progressBarFill.fillAmount = 1f;
					progressBarQueuedOutline.SetActive(false);
				}
				else
				{
					progressBarText.text = "Queued";
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
			if (other == null) Debug.LogWarning("Other is null");
			if (progressBar == null) Debug.LogWarning("progressBar is null");
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

			if(Browser.IsSubscribed(handle.modId))
			{
				progressBar.SetActive(true);
			}
			else
			{
				progressBar.SetActive(false);
			}

			progressBarFill.fillAmount = handle.Progress;
            
			switch(handle.OperationType)
			{
				case ModManagementOperationType.None_AlreadyInstalled:
					progressBar.SetActive(true);
					progressBarText.text = "Subscribed";
					break;
				case ModManagementOperationType.None_ErrorOcurred:
					break;
				case ModManagementOperationType.Install:
					progressBar.SetActive(true);
					progressBarText.text = $"Downloading";
					break;
				case ModManagementOperationType.Download:
					progressBar.SetActive(true);
					progressBarText.text = "Downloading";
					break;
				case ModManagementOperationType.Uninstall:
					break;
				case ModManagementOperationType.Update:
					progressBar.SetActive(true);
					progressBarText.text = "Updating";
					break;
			}
		}
		
		internal void UpdateStatus(ModManagementEventType updatedStatus, ModId id)
        {
	        if(profile.id != id)
	        {
		        return;
	        }
	        
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
		            progressBarText.text = "Error";
		            progressBarFill.fillAmount = 0f;
		            break;
                case ModManagementEventType.InstallStarted:
                    progressBarText.text = "Installing";
                    progressBarFill.fillAmount = 1f;
                    progressBar.SetActive(true);
                    break;
                case ModManagementEventType.Installed:
                    progressBarText.text = "Subscribed";
                    progressBarFill.fillAmount = 1f;
                    progressBar.SetActive(true);
                    break;
                case ModManagementEventType.DownloadStarted:
                    progressBarText.text = "Downloading";
                    progressBar.SetActive(true);
                    break;
                case ModManagementEventType.Downloaded:
                    progressBarText.text = "Subscribed";
                    progressBarFill.fillAmount = 1f;
                    progressBar.SetActive(true);
                    break;
                case ModManagementEventType.UpdateStarted:
                    progressBarText.text = "Updating";
                    progressBar.SetActive(true);
                    break;
                case ModManagementEventType.Updated:
                    progressBarText.text = "Subscribed";
                    progressBarFill.fillAmount = 1f;
                    progressBar.SetActive(true);
                    break;
            }
        }
	}
}
