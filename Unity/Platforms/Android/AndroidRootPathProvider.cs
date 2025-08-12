using Modio.FileIO;
using UnityEngine;

namespace Modio.Unity.Platforms.Android
{
    public class AndroidRootPathProvider : IModioRootPathProvider
    {
        public string Path => $"{Application.persistentDataPath}/UnityCache/";
        public string UserPath => $"{Application.persistentDataPath}";
    }
}
