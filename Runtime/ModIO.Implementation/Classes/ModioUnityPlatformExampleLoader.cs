using System;
using System.Linq;
using UnityEngine;

namespace ModIO.Implementation
{
    public class ModioUnityPlatformExampleLoader : MonoBehaviour
    {
        [Serializable]
        class PlatformExamples
        {
            public RuntimePlatform[] Platforms;
            public string[] PrefabNames;
        }

        [SerializeField]
        PlatformExamples[] _platformExamplesPerPlatform;

        void Awake()
        {
            var runtimePlatform = Application.platform;
            foreach (var platformExamples in _platformExamplesPerPlatform)
            {
                if (!platformExamples.Platforms.Contains(runtimePlatform))
                    continue;

                foreach (string prefabName in platformExamples.PrefabNames)
                {
                    GameObject prefab = Resources.Load<GameObject>(prefabName);
                    if (prefab != null)
                        Instantiate(prefab, transform);
                    else
                        Debug.LogError($"Couldn't find expected platformExample {prefabName} for platform {runtimePlatform}");
                }
            }
        }

        [ContextMenu("TestAllPrefabNamesAreFound")]
        void TestAllPrefabNamesAreFound()
        {
            bool issues = false;
            foreach (var platformExamples in _platformExamplesPerPlatform)
            {
                foreach (string prefabName in platformExamples.PrefabNames)
                {
                    GameObject prefab = Resources.Load<GameObject>(prefabName);
                    if (prefab == null)
                    {
                        Debug.LogError($"Couldn't find expected platformExample {prefabName} for platform {platformExamples.Platforms.FirstOrDefault()}");
                        issues = true;
                    }
                }
            }
            if (!issues)
                Debug.Log("No issues found");
        }
    }
}
