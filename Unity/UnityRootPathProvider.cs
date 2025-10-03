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
        /// Returns a value based on <c>Application.persistentDataPath</c>
        /// </returns>
        /// </summary>
        public string Path => Application.persistentDataPath;
        
        /// <summary>
        /// Path to the local user app data folder;
        /// <returns>
        /// Typically returns <c>Application.persistentDataPath</c>
        /// </returns>
        /// </summary>
        public string UserPath  => System.IO.Path.Combine(Application.persistentDataPath, "UserData");
    }
}
