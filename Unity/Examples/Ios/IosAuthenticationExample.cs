using System;
using Modio.FileIO;
using UnityEngine;

#if UNITY_IOS
using AppleAuth;
using AppleAuth.Native;
using Modio.Authentication;
using Modio.Unity.Platforms.Ios;
#endif

namespace Modio.Unity.Examples.Ios
{
    public class IosAuthenticationExample : MonoBehaviour
    {
#if UNITY_IOS
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void OnAssemblyLoaded()
        {
            if (Application.isEditor)
                return;
            BindServices();
        }
        
        static void BindServices()
        {
            ModioServices.Bind<IModioDataStorage>()
                         .FromNew<IosDataStorage>();
        }

        IAppleAuthManager _appleAuthManager;
#endif

        void Awake()
        {
#if UNITY_IOS
            try
            {
                ModioLog.Verbose?.Log($"Initializing Apple Auth");
                
                var deserializer = new PayloadDeserializer();
                _appleAuthManager = new AppleAuthManager(deserializer);

                ModioServices.Bind<IAppleAuthManager>()
                             .FromInstance(_appleAuthManager);

                ModioServices.Bind<IosAuthenticationService>()
                             .WithInterfaces<IModioAuthService, IGetActiveUserIdentifier>()
                             .FromNew<IosAuthenticationService>();
                
                ModioLog.Verbose?.Log($"Successfully initialized Apple Auth");
            }
            catch (Exception e)
            {
                ModioLog.Error?.Log(e);
            }
#else
            Destroy(gameObject);
#endif
        }

#if UNITY_IOS
        void Update() => _appleAuthManager?.Update();
#endif
    }
}
