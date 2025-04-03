using System;
using System.IO;
using Modio.API.Interfaces;

namespace Modio.FileIO
{
    /// <summary>
    /// Provides a root path for windows 
    /// </summary>
    public class WindowsRootPathProvider : IModioRootPathProvider
    {
        /// <summary>
        /// Is the required environment variable set.
        /// <returns>
        /// True if the environment variable "public" is set, false otherwise.
        /// </returns>
        /// </summary>
        public static bool IsPublicEnvironmentVariableSet() => Environment.GetEnvironmentVariable("public") != null;

        /// <summary>
        /// Path to the shared public folder;
        /// <returns>
        /// Typically returns "C:\Users\Public\"
        /// </returns>
        /// </summary>
        public string Path => $"{Environment.GetEnvironmentVariable("public")}";
        
        /// <summary>
        /// Path to the local user app data folder;
        /// <returns>
        /// Typically returns "C:\Users\&lt;UserName&gt;\AppData\Roaming"
        /// </returns>
        /// </summary>
        public string UserPath  => $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}";
    }
}
