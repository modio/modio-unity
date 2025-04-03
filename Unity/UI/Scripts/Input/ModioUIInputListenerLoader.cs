using UnityEngine;

namespace Modio.Unity.UI.Input
{
    public class ModioUIInputListenerLoader : MonoBehaviour
    {
        [SerializeField]
        string[] _prefabNames;

        [SerializeField]
        string _fallbackPrefabName;

        void Awake()
        {
            bool needsFallback = true;
            foreach (string prefabName in _prefabNames)
            {
                GameObject prefab = Resources.Load<GameObject>(prefabName);
                if (prefab == null) continue;
                Instantiate(prefab, transform);
                needsFallback = false;
            }

            if (needsFallback && !string.IsNullOrEmpty(_fallbackPrefabName))
            {
                GameObject prefab = Resources.Load<GameObject>(_fallbackPrefabName);
                if (prefab != null)
                    Instantiate(prefab, transform);
            }
        }
    }
}
