using System.Collections.Generic;
using System.IO;
using System.Linq;
using Modio.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Modio.Unity.Editor
{
    public static class EditSettingsTool
    {
        [MenuItem("Tools/mod.io/Edit Settings")]
        public static void EditSettings()
        {
            var settings = GetSettingsAsset();

            EditorGUIUtility.PingObject(settings);
            Selection.activeObject = settings;
        }

        internal static ModioUnitySettings GetSettingsAsset()
        {
            var settingsAsset = Resources.Load<ModioUnitySettings>(ModioUnitySettings.DefaultResourceName);

            // if it doesn't exist we create one
            if (settingsAsset)
                return settingsAsset;

            // create asset
            settingsAsset = ScriptableObject.CreateInstance<ModioUnitySettings>();
            
            CreateAssetAtPath(settingsAsset, "Assets", "Resources", ModioUnitySettings.DefaultResourceName);

            AssetDatabase.Refresh();

            return settingsAsset;
        }

        static void CreateAssetAtPath(ModioUnitySettings settingsAsset, params string[] paths)
        {

            List<string> pathList = paths.SelectMany(t => t.Split('/')).ToList();

            string currentPath = pathList[0];

            
            for (var i = 1; i < pathList.Count - 1; i++)
            {
                
                if (!AssetDatabase.IsValidFolder(Path.Combine(currentPath, pathList[i])))
                    AssetDatabase.CreateFolder(currentPath, pathList[i]);

                currentPath = Path.Combine(currentPath, pathList[i]);
            }

            currentPath = Path.Combine(currentPath, pathList[^1]);

            AssetDatabase.CreateAsset(settingsAsset, $"{currentPath}.asset");

        }
    }
}
