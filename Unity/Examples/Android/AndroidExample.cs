using Modio.FileIO;
using Modio.Unity.Platforms.Android;
using UnityEngine;

namespace Modio.Unity.Examples.Android
{
    public class AndroidExample : MonoBehaviour
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void OnAssemblyLoaded()
        {
            ModioServices.Bind<IModioRootPathProvider>()
                         .FromNew<AndroidRootPathProvider>();
            
            ModioServices.Bind<IModioDataStorage>()
                         .FromNew<AndroidDataStorage>();
        }
#endif
    }
}
