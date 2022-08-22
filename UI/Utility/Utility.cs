using System;
using ModIO;

namespace ModIOBrowser.Implementation
{

    /// <summary>
    /// Generic utility class for repeated calculations or operations.
    /// </summary>
    internal static class Utility
    {
        /// <summary>
        /// changes an int64 number into something more human readable such as "12.6K"
        /// </summary>
        /// <param name="number">the long to convert to readable string</param>
        /// <returns></returns>
        public static string GenerateHumanReadableNumber(long number)
        {
            if(number >= 1000000)
            {
                float divided = number / 1000000f;
                return divided.ToString("0.0") + "M";
            }
            if(number >= 1000)
            {
                float divided = number / 1000f;
                return divided.ToString("0.0") + "K";
            }
            return number.ToString();
        }

        public static string GenerateHumanReadableTimeStringFromSeconds(int seconds)
        {
            if(seconds < 60f)
            {
                return seconds.ToString() + " seconds";
            } 
            else if(seconds < 3600)
            {
                int minutes = seconds / 60;
                int remainingSeconds = seconds % 60;

                return $"{minutes}:{remainingSeconds:00}";
            }
            else
            {
                int hours = seconds / 3600;
                int minutes = seconds % 3600 / 60;
                int remainingSeconds = seconds % 3600 % 60;

                return $"{hours}:{minutes:00}:{remainingSeconds:00}";
            }
        }

        public static string GenerateHumanReadableStringForBytes(long bytes)
        {
            if(bytes > 1048576)
            {
                return (bytes / 1048576f).ToString("0.0") + "MB";
            } 
            else
            {
                return (bytes / 1024).ToString("0.0") + "KB";
            }
        }
        
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

        public static int GetIndex(int current, int length, int change)
        {
            if(length == 0)
            {
                return 0;
            }
            
            current += change;
            
            while(current >= length)
            {
                current -= length;
            }
            while(current < 0)
            {
                current += length;
            }
            
            return current;
        }

        #region Comparer<T> delegates for sorting a List<ModProfile> via List<T>.Sort()
        public static int CompareModProfilesAlphabetically(SubscribedMod A, SubscribedMod B)
        {
            return CompareModProfilesAlphabetically(A.modProfile, B.modProfile);
        }
        public static int CompareModProfilesAlphabetically(InstalledMod A, InstalledMod B)
        {
            return CompareModProfilesAlphabetically(A.modProfile, B.modProfile);
        }

        public static int CompareModProfilesAlphabetically(ModProfile A, ModProfile B)
        {
            float valueOfA = 0;
            float valueOfB = 0;
            float depthMultiplier = 0;
            int maxDepth = 10;
            int depth = 0;

            foreach(char character in A.name)
            {
                if(depth >= maxDepth)
                {
                    break;
                } 
                depthMultiplier = depthMultiplier == 0 ? 1 : depthMultiplier + 100;
                valueOfA += char.ToLower(character) / depthMultiplier;
                depth++;
            }
            
            depthMultiplier = 0;
            depth = 0;

            foreach(char character in B.name)
            {
                if(depth >= maxDepth)
                {
                    break;
                } 
                depthMultiplier = depthMultiplier == 0 ? 1 : depthMultiplier + 100;
                valueOfB += char.ToLower(character) / depthMultiplier;
                depth++;
            }
            if(valueOfA > valueOfB)
            {
                return 1;
            } 
            if(valueOfB > valueOfA)
            {
                return -1;
            }
            return 0;
        }

        public static int CompareModProfilesByFileSize(SubscribedMod A, SubscribedMod B)
        {
            return CompareModProfilesByFileSize(A.modProfile, B.modProfile);
        }

        public static int CompareModProfilesByFileSize(InstalledMod A, InstalledMod B)
        {
            return CompareModProfilesByFileSize(A.modProfile, B.modProfile);
        }

        public static int CompareModProfilesByFileSize(ModProfile A, ModProfile B)
        {
            if(A.archiveFileSize > B.archiveFileSize)
            {
                return -1;
            }
            if(A.archiveFileSize < B.archiveFileSize)
            {
                return 1;
            }
            return 0;
        }
        #endregion Comparer<T> delegates for sorting a List<ModProfile> via List<T>.Sort()
        
        #region Get a mod status in string format
        
        public static string GetModStatusAsString(ModManagementEventType updatedStatus)
        {
            switch(updatedStatus)
            {
                case ModManagementEventType.InstallStarted:
                    return "Installing";
                case ModManagementEventType.Installed:
                    return "Installed";
                case ModManagementEventType.InstallFailed:
                    return "Problem occurred";
                case ModManagementEventType.DownloadStarted:
                    return "Downloading";
                case ModManagementEventType.Downloaded:
                    return "Ready to install";
                case ModManagementEventType.DownloadFailed:
                    return "Problem occurred";
                case ModManagementEventType.UninstallStarted:
                    return "Uninstalling";
                case ModManagementEventType.Uninstalled:
                    return "Uninstalled";
                case ModManagementEventType.UninstallFailed:
                    return "Problem occurred";
                case ModManagementEventType.UpdateStarted:
                    return "Updating";
                case ModManagementEventType.Updated:
                    return "Installed";
                case ModManagementEventType.UpdateFailed:
                    return "Problem occurred";
            }
            return "";
        }

        public static string GetModStatusAsString(ProgressHandle handle)
        {
            switch(handle.OperationType)
            {
                case ModManagementOperationType.None_AlreadyInstalled:
                    return "Installed";
                case ModManagementOperationType.None_ErrorOcurred:
                    return "<color=red>Problem occurred</color>";
                case ModManagementOperationType.Install:
                    return $"Installing {(int)(handle.Progress * 100)}%";
                case ModManagementOperationType.Download:
                    return $"Downloading {(int)(handle.Progress * 100)}%";
                case ModManagementOperationType.Uninstall:
                    return "Uninstalling";
                case ModManagementOperationType.Update:
                    return $"Updating {(int)(handle.Progress * 100)}%";
            }
            return "";
        }

        public static string GetModStatusAsString(SubscribedMod mod)
        {
            switch(mod.status)
            {
                case SubscribedModStatus.Installed:
                    return "Installed";
                case SubscribedModStatus.WaitingToDownload:
                    return "Waiting to download";
                case SubscribedModStatus.WaitingToInstall:
                    return "Waiting to install";
                case SubscribedModStatus.WaitingToUpdate:
                    return "Waiting to Update";
                case SubscribedModStatus.Downloading:
                    return "Downloading";
                case SubscribedModStatus.Installing:
                    return "Installing";
                case SubscribedModStatus.Uninstalling:
                    return "Deleting";
                case SubscribedModStatus.Updating:
                    return "Updating";
                case SubscribedModStatus.ProblemOccurred:
                    return "Problem occurred";
                default:
                    return "";
            }
        }
        #endregion
    }
}
