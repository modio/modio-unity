using System;
using System.Collections.Generic;
using System.IO;
using ModIO.Implementation.Platform;
using UnityEngine;

namespace ModIO.Util
{
    public static class Utility
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


        /// <summary>
        /// Finds everything in a loaded scene. Slow.
        /// </summary>
        public static List<T> FindEverythingInScene<T>() where T : Component
        {
            List<T> results = new List<T>();
            T[] components = Resources.FindObjectsOfTypeAll<T>();
            foreach(T component in components)
            {
                if(component.gameObject.scene.isLoaded)
                {
                    results.Add(component);
                }
            }
            return results;
        }

        /// <summary>
        /// Overrides the current platform setting in rest api calls
        /// </summary>
        /// <param name="platform">new rest api platform</param>
        public static void ForceSetPlatformHeader(RestApiPlatform platform)
        {
            PlatformConfiguration.RESTAPI_HEADER = platform.ToString();
        }

        /// <summary>Downloads a mod's file archive into <paramref name="archiveStream"/>.<br />Use the returned <see cref="DownloadModToStreamOperation"/> to track status and extract files.</summary>
        /// <param name="modId">The id of the mod to download.</param>
        /// <param name="archiveStream">The stream the compressed archive will be downloaded into.</param>
        /// <param name="closeStream">Should the operation close <paramref name="archiveStream"/> when it is disposed?</param>
        /// <returns>Operation handle to access status and results.</returns>
        /// <example><code>
        /// async void DownloadModToMemory(ModId modId)
        /// {
        ///     using MemoryStream archiveStream = new MemoryStream();
        /// <br /> <br />
        ///     DownloadModToStreamOperation op = Utility.DownloadModToStream(modId, archiveStream);
        ///     while (!op.task.IsCompleted)
        ///     {
        ///         if (op.IsDownloading)
        ///             Debug.Log($"Download progress: {op.DownloadProgress}");
        /// <br /> <br />
        ///         await Task.Yield();
        ///     }
        /// <br /> <br />
        ///     List&lt;MemoryStream&gt; fileStreams = new List&lt;MemoryStream&gt;();
        /// <br /> <br />
        ///     foreach (DownloadModToStreamOperation.ArchiveStreamFile file in op.GetFiles())
        ///     {
        ///         MemoryStream fileStream = new MemoryStream();
        ///         await op.ExtractFileToStream(file, fileStream);
        ///         fileStreams.Add(fileStream);
        ///     }
        /// }
        /// </code></example>
        /// <seealso cref="DownloadModToStreamOperation.GetFiles"/>
        /// <seealso cref="DownloadModToStreamOperation.ArchiveStreamFile"/>
        /// <seealso cref="DownloadModToStreamOperation.ExtractFileToStream(DownloadModToStreamOperation.ArchiveStreamFile, Stream, bool)"/>
        public static DownloadModToStreamOperation DownloadModToStream(ModId modId, Stream archiveStream, bool closeStream = true) => new DownloadModToStreamOperation(modId, archiveStream, closeStream);
    }
}
