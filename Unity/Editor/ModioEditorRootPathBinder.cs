using Modio.FileIO;
using UnityEditor;

namespace Modio.Unity.Editor
{
    public static class ModioEditorRootPathBinder
    {
        [InitializeOnLoadMethod]
        public static void BindRootPathProviderIfNoneBound()
        {
#if UNITY_EDITOR_WIN
            ModioServices.Bind<IModioRootPathProvider>()
                         .FromNew<WindowsRootPathProvider>(
                             ModioServicePriority.PlatformProvided,
                             WindowsRootPathProvider.IsPublicEnvironmentVariableSet
                         );
#else
            ModioServices.Bind<IModioRootPathProvider>()
                         .FromNew<UnityRootPathProvider>(ModioServicePriority.Default);
#endif
        }
    }
}
