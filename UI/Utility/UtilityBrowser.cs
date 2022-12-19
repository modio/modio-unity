using System;
using System.Collections.Generic;
using ModIO;
using UnityEngine;

namespace ModIOBrowser.Implementation
{

    /// <summary>
    /// Generic utility class for repeated calculations or operations.
    /// </summary>
    internal static class UtilityBrowser
    {
        public static string GetModStatusAsString(ModManagementEventType updatedStatus)
        {
            switch(updatedStatus)
            {
                case ModManagementEventType.InstallStarted:
                    return TranslationManager.Instance.Get("Installing");
                case ModManagementEventType.Installed:
                    return TranslationManager.Instance.Get("Installed");
                case ModManagementEventType.InstallFailed:
                    return TranslationManager.Instance.Get("Problem occurred");
                case ModManagementEventType.DownloadStarted:
                    return TranslationManager.Instance.Get("Downloading");
                case ModManagementEventType.Downloaded:
                    return TranslationManager.Instance.Get("Ready to install");
                case ModManagementEventType.DownloadFailed:
                    return TranslationManager.Instance.Get("Problem occurred");
                case ModManagementEventType.UninstallStarted:
                    return TranslationManager.Instance.Get("Uninstalling");
                case ModManagementEventType.Uninstalled:
                    return TranslationManager.Instance.Get("Uninstalled");
                case ModManagementEventType.UninstallFailed:
                    return TranslationManager.Instance.Get("Problem occurred");
                case ModManagementEventType.UpdateStarted:
                    return TranslationManager.Instance.Get("Updating");
                case ModManagementEventType.Updated:
                    return TranslationManager.Instance.Get("Installed");
                case ModManagementEventType.UpdateFailed:
                    return TranslationManager.Instance.Get("Problem occurred");
            }
            return "";
        }

        public static string GetModStatusAsString(ProgressHandle handle)
        {
            switch(handle.OperationType)
            {
                case ModManagementOperationType.None_AlreadyInstalled:
                    return TranslationManager.Instance.Get("Installed");
                case ModManagementOperationType.None_ErrorOcurred:
                    return TranslationManager.Instance.Get("{color}Problem occurred<endcolor>", "<color=red>", "</color>");
                case ModManagementOperationType.Install:
                    return TranslationManager.Instance.Get("Installing (progress}%", $"{(int)(handle.Progress * 100)}");
                case ModManagementOperationType.Download:
                    return TranslationManager.Instance.Get("Downloading {progress}%", $"{(int)(handle.Progress * 100)}");
                case ModManagementOperationType.Uninstall:
                    return TranslationManager.Instance.Get("Uninstalling");
                case ModManagementOperationType.Update:
                    return TranslationManager.Instance.Get("Updating {progress}%", $"{(int)(handle.Progress * 100)}");
            }
            return "";
        }

        public static string FullPath(Transform t)
        {            
            Transform current = t;
            string output = current.name;

            while(current != null)
            {
                if(current.parent == null)
                {
                    return output;
                }
                output = current.parent.name + "\\" + output;
                current = current.parent;
            }

            return output;
        }

        public static IEnumerable<string> FullPathForMultiple<T>(List<T> gos) where T : MonoBehaviour
        {
            foreach(var item in gos) yield return $"{UtilityBrowser.FullPath(item.transform)}\n";
        }
    }
}
