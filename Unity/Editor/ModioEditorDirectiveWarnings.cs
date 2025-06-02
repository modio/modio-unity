using UnityEditor;
using UnityEngine;

namespace Modio.Editor.Unity
{
    /// <summary>
    /// Logs warnings in the Unity Editor if deprecated scripting define symbols are present.
    /// </summary>
    [InitializeOnLoad]
    public static class ModioEditorDirectiveWarnings
    {
        static ModioEditorDirectiveWarnings()
        {
            LogFacepunchWarning();
            LogSteamworksWarning();
        }

        static void LogFacepunchWarning()
        {
#if UNITY_FACEPUNCH && !MODIO_FACEPUNCH
            Debug.LogWarning("Modio: 'UNITY_FACEPUNCH' define has been deprecated. Please use 'MODIO_FACEPUNCH' instead.");
#endif
        }

        static void LogSteamworksWarning()
        {
#if UNITY_STEAMWORKS && !MODIO_STEAMWORKS
            Debug.LogWarning("Modio: 'UNITY_STEAMWORKS' define has been deprecated. Please use 'MODIO_STEAMWORKS' instead.");
#endif
        }
    }
}
