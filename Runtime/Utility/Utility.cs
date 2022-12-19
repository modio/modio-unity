using System;
using UnityEngine;

namespace ModIO
{
    public partial class Utility
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

        /// <summary>
        /// You can use this to convert your byte[] steam app ticket into a trimmed base64 encoded
        /// string to be used for the steam authentication.
        /// </summary>
        /// <param name="ticketData">the byte[] steam app ticket data</param>
        /// <param name="ticketSize">the desired length of the ticket to be trimmed to</param>
        /// <seealso cref="SetupSteamAuthenticationOption"/>
        /// <returns>base 64 encoded string from the provided steam app ticket</returns>
        public static string EncodeEncryptedSteamAppTicket(byte[] ticketData, uint ticketSize)
        {
            //--------------------------- Assert correct parameters ------------------------------//
            Debug.Assert(ticketData != null);
            Debug.Assert(ticketData.Length > 0 && ticketData.Length <= 1024,
                "[mod.io Browser] Invalid ticketData length. Make sure you have a valid "
                + "steam app ticket");
            Debug.Assert(ticketSize > 0 && ticketSize <= ticketData.Length,
                "[mod.io Browser] Invalid ticketSize. The ticketSize cannot be larger than"
                + " the length of the app ticket and must be greater than zero.");

            //------------------------------- Trim the app ticket --------------------------------//
            byte[] trimmedTicket = new byte[ticketSize];
            Array.Copy(ticketData, trimmedTicket, ticketSize);

            string base64Ticket = null;
            try
            {
                base64Ticket = Convert.ToBase64String(trimmedTicket);
            }
            catch(Exception exception)
            {
                Debug.LogError($"[mod.io Browser] Unable to convert the app ticket to a "
                               + $"base64 string, caught exception: {exception.Message} - "
                               + $"{exception.InnerException?.Message}");
            }

            return base64Ticket;
        }
    }
}
