using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public static class CacheOpener
{
    [MenuItem("Tools/mod.io/Open Cache")]
    public static void OpenCache()
    {
        try
        {
            string path = Path.GetFullPath($"{Application.persistentDataPath}/mod.io");
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
            Debug.LogError($"Exception opening local cache: {exception.Message}\n{exception.StackTrace}");
        }
    }
}
#endif
