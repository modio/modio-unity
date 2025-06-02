using Modio.FileIO;
using UnityEngine;

namespace Modio.Unity
{
    /// <summary>
    /// Provides a default root path for non-windows Unity platforms.
    /// </summary>
    public class UnityRootPathProvider : IModioRootPathProvider
    {
        /// <summary>
        /// Path to the shared public folder;
        /// <returns>
        /// Typically returns "C:\Users\Public\"
        /// </returns>
        /// </summary>
        public string Path => Application.persistentDataPath;
        
        /// <summary>
        /// Path to the local user app data folder;
        /// <returns>
        /// Typically returns "C:\Users\&lt;UserName&gt;\AppData\Roaming"
        /// </returns>
        /// </summary>
        public string UserPath  => System.IO.Path.Combine(Application.persistentDataPath, "UserData");
    }
}
