using UnityEngine;

#if MODIO_OCULUS
using Oculus.Platform;
using Oculus.Platform.Models;
#endif

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformExampleOculus : MonoBehaviour
    {
#if MODIO_OCULUS
        void Awake()
        {
            // Oculus takes a fair bit of time to initialize and perform the entitlement check. By calling this
            // we're telling the plugin to wait until it's ready.
            ModioPlatform.SetInitializing();

            // Ensure you have your game's App Id set up in the Oculus Settings object provided by the Meta Platform SDK
            Core.AsyncInitialize().OnComplete(OnInitialize);
        }

        void OnInitialize(Message<PlatformInitialize> message)
        {
            if (message.IsError)
            {
                Logger.Log(LogLevel.Error, $"initializing Oculus Platform: {message.GetError().Message}");
                return;
            }

            Logger.Log(LogLevel.Verbose, $"Oculus Platform initialized successfully");

            Entitlements.IsUserEntitledToApplication().OnComplete(OnEntitled);
        }

        void OnEntitled(Message message)
        {
            if (message.IsError)
            {
                Logger.Log(LogLevel.Error, $"Error checking Oculus Platform entitlement: {message.GetError().Message}");
                return;
            }

            Logger.Log(LogLevel.Verbose, $"Oculus Platform verified entitlement successfully");

            ModioPlatformOculus.SetAsPlatform();
        }
#endif
    }
}
