using System.Threading.Tasks;
using Modio.API;
using Modio.Authentication;
using Modio.Extensions;
using Plugins.Modio.Unity.Platforms.Android;
using UnityEngine;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace Modio.Unity.Examples.Android
{
    public class GooglePlayGamesExample : MonoBehaviour
    {
        void Awake()
        {
            // This will be instantiated on Oculus, but we want to early out here to avoid interfering with Oculus
#if MODIO_OCULUS
            Destroy(gameObject);
            return;
#endif
            if (Application.platform != RuntimePlatform.Android)
            {
                Destroy(gameObject);
                return;
            }
            
            // `IGetPortalProvider` is needed before authentication for API configuration, so this lives in a separate
            // place
            ModioServices.Bind<GoogleGamesPortalProvider>()
                         .WithInterfaces<IGetPortalProvider>()
                         .FromNew<GoogleGamesPortalProvider>();
            
            ModioLog.Verbose?.Log("Attempting Google Play Games sign-in");
            
            SignInGooglePlayGames().ForgetTaskSafely();
        }

        async Task SignInGooglePlayGames()
        {
#if UNITY_ANDROID
            var signInTcs = new TaskCompletionSource<SignInStatus>();
            
            PlayGamesPlatform.DebugLogEnabled = true;
            PlayGamesPlatform.Activate();
            PlayGamesPlatform.Instance.Authenticate(signInTcs.SetResult);
            
            var result = await signInTcs.Task;

            if (result != SignInStatus.Success)
            {
                ModioLog.Error?.Log($"Error signing into Google Play Games: {result}. Please restart the app or try again");
                return;
            }
            
            ModioServices.Bind<GoogleGamesAuthService>()
                         .WithInterfaces<IModioAuthService, IGetActiveUserIdentifier>()
                         .FromNew<GoogleGamesAuthService>();
#endif
        }
    }

    public class GoogleGamesPortalProvider : IGetPortalProvider
    {
        public ModioAPI.Portal Portal => ModioAPI.Portal.Google;
    }
}
