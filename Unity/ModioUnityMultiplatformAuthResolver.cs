using System.Threading.Tasks;
using Modio.Authentication;
using Modio.Images;
using Modio.Unity.Settings;
using UnityEngine;

namespace Modio.Unity
{
    public class ModioUnityMultiplatformAuthResolver : ModioMultiplatformAuthResolver,
                                                       IExternalAvatarProviderService<Texture2D>
    {
        public static bool IsSupportedPlatform => !Application.isConsolePlatform
                                                  && !Application.isMobilePlatform
                                                  && ModioServices.TryResolve(out ModioSettings settings)
                                                  && settings.TryGetPlatformSettings(out ModioComponentUISettings cuiSettings)
                                                  && cuiSettings.EnableAuthSelection;

        public ModioUnityMultiplatformAuthResolver() : base()
        {
            ModioServices.Bind<IExternalAvatarProviderService<Texture2D>>()
                         .FromInstance(this, SERVICE_BINDING_PRIORITY);
        }

        public Task<(Error error, Texture2D image)> TryGetAvatarImage()
            => Get<IModioAuthService>() is IExternalAvatarProviderService<Texture2D> service
                ? service.TryGetAvatarImage()
                : Task.FromResult<(Error error, Texture2D image)>((Error.None, null));
    }
}
