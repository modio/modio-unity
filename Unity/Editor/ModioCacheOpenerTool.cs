using System;
using System.IO;
using Modio.FileIO;
using UnityEditor;

namespace Modio.Unity.Editor
{
    public static class ModioCacheOpenerTool
    {
        [MenuItem("Tools/mod.io/Open Cache")]
        public static void OpenCache()
        {
            if (!ModioServices.TryResolve(out IModioRootPathProvider pathProvider))
            {
                ModioLog.Warning?.Log($"No {nameof(IModioRootPathProvider)} bound, cannot open cache!");
                return;
            }
            
            try
            {
                // Cus editor tool we simply use Unity's path
                string path = Path.GetFullPath($"{pathProvider.Path}/mod.io");
#if UNITY_EDITOR_WIN || UNITY_EDITOR_LINUX
                // Supposedly Linux uses the same executable name as windows, though not 100% confident
                // so wrapping all this in a try catch.
                System.Diagnostics.Process.Start("explorer.exe", path);
#elif UNITY_EDITOR_OSX
                System.Diagnostics.Process.Start("open", $"-R \"{path}\"");
#endif
            }
            catch (Exception exception)
            {
                ModioLog.Error?.Log(exception);
            }
        }
    }
}
