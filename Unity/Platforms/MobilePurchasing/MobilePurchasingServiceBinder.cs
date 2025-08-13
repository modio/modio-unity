using Modio.Monetization;
using UnityEngine;

namespace Modio.Unity.Platforms.MobilePurchasing
{
    public static class MobilePurchasingServiceBinder
    {
#if MODIO_MOBILE_IAP
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void OnAssemblyLoaded()
        {
            ModioServices.Bind<ModioMobileStoreService>()
                         .WithInterfaces<IModioVirtualCurrencyProviderService>()
                         .FromNew<ModioMobileStoreService>();
        }
#endif
    }
}
