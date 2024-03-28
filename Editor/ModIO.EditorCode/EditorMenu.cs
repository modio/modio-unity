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

            EditorGUIUtility.PingObject(settingsAsset);
            Selection.activeObject = settingsAsset;
        }


        internal static SettingsAsset GetConfigAsset()
        {
            var settingsAsset = Resources.Load<SettingsAsset>(SettingsAsset.FilePath);

            // if it doesnt exist we create one
            if(settingsAsset == null)
            {
                // create asset
                settingsAsset = ScriptableObject.CreateInstance<SettingsAsset>();

                // ensure the directories exist before trying to create the asset
                if(!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                if(!AssetDatabase.IsValidFolder("Assets/Resources/mod.io"))
                {
                    AssetDatabase.CreateFolder("Assets/Resources", "mod.io");
                }

                AssetDatabase.CreateAsset(settingsAsset, $@"Assets/Resources/{SettingsAsset.FilePath}.asset");

                //create a data representation of the Settings Asset
                SerializedObject so = new SerializedObject(settingsAsset);

                //Find properties and apply default values
                SerializedProperty serverSettingsProperty = so.FindProperty("serverSettings");
                serverSettingsProperty.FindPropertyRelative("serverURL").stringValue = SettingsAssetEditor.GetURLProduction(0);
                serverSettingsProperty.FindPropertyRelative("languageCode").stringValue = "en";

                //Apply new values while ensuring the user cannot use "undo" to erase the initial values.
                so.ApplyModifiedPropertiesWithoutUndo();

                //Grab any asset changes and unload unused assets
                AssetDatabase.Refresh();
            }

            return settingsAsset;
        }

        [MenuItem("Tools/mod.io/Debug/Clear Data", false, 0)]
        public static void ClearStoredData()
        {
            // Only used for the editor
            SystemIOWrapper.DeleteDirectory(EditorDataService.TempRootDirectory);
            SystemIOWrapper.DeleteDirectory(EditorDataService.UserRootDirectory);
            SystemIOWrapper.DeleteDirectory(EditorDataService.PersistentDataRootDirectory);
        }
    }
}
#endif
