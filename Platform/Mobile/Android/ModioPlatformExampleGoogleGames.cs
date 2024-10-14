using UnityEngine;

#if UNITY_ANDROID
using GooglePlayGames;
#endif

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformExampleGoogleGames : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_ANDROID
            // Enable debug logging for Play Games Platform
            PlayGamesPlatform.DebugLogEnabled = true;
            PlayGamesPlatform.Activate();
            
            // Authenticate with Play Games Services
            PlayGamesPlatform.Instance.Authenticate((success) =>
            {
                // Check if authentication was successful
                if (success)
                {
                    // --- This is the important line to include in your own implementation ---
                    ModioPlatformGoogleGames.SetAsPlatform();
                }
                else
                {
                    // Handle unsuccessful authentication:
                    // Disable your integration with Play Games Services or show a login button to ask users to sign-in.
                    // Clicking it should call PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication);
                }
            });
#endif
        }

    }
}
