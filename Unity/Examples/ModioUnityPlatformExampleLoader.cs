using System;
using System.Linq;
using UnityEngine;

namespace Modio.Unity.Examples
{
    public class ModioUnityPlatformExampleLoader : MonoBehaviour
    {
        [Serializable]
        class PlatformExamples
        {
            public RuntimePlatform[] platforms;
            public string[] prefabNames;
        }
        
        [SerializeField]
        PlatformExamples[] platformExamplesPerPlatform;

        void Awake()
        {
            RuntimePlatform runtimePlatform = Application.platform;
            foreach (PlatformExamples platformExamples in platformExamplesPerPlatform)
            {
                if (!platformExamples.platforms.Contains(runtimePlatform))
                    continue;

                foreach (string prefabName in platformExamples.prefabNames)
                {
                    var prefab = Resources.Load<GameObject>(prefabName);
                    if (prefab != null)
                    {
                        Debug.Log($"Instantiating platform {prefabName} for platform {runtimePlatform}");
                        Instantiate(prefab, transform);
                    }
                    else
                        Debug.LogError($"Couldn't find expected platformExample {prefabName} for platform {runtimePlatform}");
                }
            }
        }

        [ContextMenu("TestAllPrefabNamesAreFound")]
        void TestAllPrefabNamesAreFound()
        {
            var issues = false;
            foreach (PlatformExamples platformExamples in platformExamplesPerPlatform)
            {
                foreach (string prefabName in platformExamples.prefabNames)
                {
                    var prefab = Resources.Load<GameObject>(prefabName);
                    if (prefab == null)
                    {
                        Debug.LogError($"Couldn't find expected platformExample {prefabName} for platform {platformExamples.platforms.FirstOrDefault()}");
                        issues = true;
                    }
                }
            }
            if (!issues)
                Debug.Log("No issues found");
        }
    }
}
