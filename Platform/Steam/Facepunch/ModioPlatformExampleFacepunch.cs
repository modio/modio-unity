#if UNITY_FACEPUNCH
using Steamworks;
#endif
using UnityEngine;

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformExampleFacepunch : MonoBehaviour
    {
        [SerializeField] int appId;

        void Awake()
        {
            bool supportedPlatform = !Application.isConsolePlatform;

#if !UNITY_FACEPUNCH
            supportedPlatform = false;
#endif

            if (!supportedPlatform)
            {
                Destroy(this);
                return;
            }

#if UNITY_FACEPUNCH
            try
            {
                SteamClient.Init((uint)appId, true);
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
                return;
            }
#endif

            // --- This is the important line to include in your own implementation ---
            ModioPlatformFacepunch.SetAsPlatform();
        }

#if UNITY_FACEPUNCH
        void OnDisable()
        {
            SteamClient.Shutdown();
        }

        void Update()
        {
            SteamClient.RunCallbacks();
        }
#endif
    }
}
