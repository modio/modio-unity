using Modio.FileIO;
using UnityEngine;

namespace Plugins.Modio.Unity.Examples.Android
{
    public class AndroidRootPathProvider : IModioRootPathProvider
    {
        public string Path => $"{Application.persistentDataPath}";
        public string UserPath => Path;
    }
}
