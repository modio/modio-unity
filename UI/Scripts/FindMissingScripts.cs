#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class FindMissingScripts : MonoBehaviour
{
    [MenuItem("Tools/Find Missing Scripts In Project Menu")]
    static void FindMissingScriptsInProjectMenu()
    {
        string[] prefabPaths = AssetDatabase.GetAllAssetPaths().Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)).ToArray();

        foreach(var path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var component in prefab.GetComponentsInChildren<Component>())
            {
                if(component == null)
                {
                    Debug.Log("Prefab found with missing script " + path, prefab);
                    break;
                }
            }
        }
        Debug.Log("Completed Search");
    }

    [MenuItem("Tools/Find Missing Scripts In Scene Menu")]
    static void FindMissingScriptsInSceneMenuItem()
    {
        foreach(var go in GameObject.FindObjectsOfType<GameObject>(true))
        {
            foreach(var component in go.GetComponentsInChildren<Component>())
            {
                if(component == null)
                {
                    Debug.Log("Prefab found with missing script " + go.name, go);
                    break;
                }
            }
        }
        Debug.Log("Completed Search");
    }
}
#endif
