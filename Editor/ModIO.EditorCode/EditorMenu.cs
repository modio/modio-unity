#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using ModIO.Implementation;
using ModIO.Implementation.Platform;

namespace ModIO.EditorCode
{
    /// <summary>summary</summary>
    public static class EditorMenu
    {
        static EditorMenu()
        {
            new MenuItem("Tools/mod.io/Edit Settings", false, 0);
        }

        [MenuItem("Tools/mod.io/Edit Settings", false, 0)]
        public static void EditSettingsAsset()
        {
            var settingsAsset = GetConfigAsset();

            UnityEditor.EditorGUIUtility.PingObject(settingsAsset);
            UnityEditor.Selection.activeObject = settingsAsset;
        }

        internal static SettingsAsset GetConfigAsset()
        {
            var settingsAsset = Resources.Load<SettingsAsset>(SettingsAsset.FilePath);

            // if it doesnt exist we create one
            if(settingsAsset == null)
            {
                // create asset
                settingsAsset = ScriptableObject.CreateInstance<SettingsAsset>();
                settingsAsset.serverSettings.serverURL = "https://api.mod.io/v1";
                settingsAsset.serverSettings.languageCode = "en";

                // ensure the directories exist before trying to create the asset
                if(!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                }
                if(!AssetDatabase.IsValidFolder("Assets/Resources/mod.io"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "mod.io");
                }

                UnityEditor.AssetDatabase.CreateAsset(
                    settingsAsset, $@"Assets/Resources/{SettingsAsset.FilePath}.asset");
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }

            return settingsAsset;
        }

        [MenuItem("Tools/mod.io/Debug/Clear Data", false, 0)]
        public static void ClearStoredData()
        {
            SystemIOWrapper.DeleteDirectory(EditorDataService.GlobalRootDirectory);
        }
    }
}
#endif
