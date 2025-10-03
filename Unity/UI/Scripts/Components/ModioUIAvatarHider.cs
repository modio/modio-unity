using System;
using Modio.Users;
using UnityEngine;

namespace Modio.Unity.UI.Scripts.Components
{
    public class ModioUIAvatarHider : MonoBehaviour
    {
        void Awake()
        {
            ModioClient.OnInitialized += OnPluginReady;
        }

        void OnDestroy()
        {
            ModioClient.OnInitialized -= OnPluginReady;
        }

        void OnPluginReady() => gameObject.SetActive(ModioUnityMultiplatformAuthResolver.IsSupportedPlatform);
    }
}
