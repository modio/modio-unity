using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modio.API;
using Modio.Errors;
using Modio.Mods;
using Modio.Users;

namespace Modio.Monetization
{
    public static class ModioFiatPrice
    {
        static ModioAPI.Portal _currentPortal = ModioAPI.Portal.None;
        static bool _cached = false;
        static Task<Error> _updateSkuCacheTask;
        static Action _onSkuUpdated;
        static readonly HashSet<Mod> ModsWithPrice = new HashSet<Mod>();
        
        static void Reset()
        {
            _currentPortal = ModioAPI.Portal.None;
            _cached = false;
            _updateSkuCacheTask = null;
        }

        public static async Task<Error> FetchSkuCache(ModioAPI.Portal portal)
        {

            if (!ModioServices.TryResolve(out IModioUsdMarketplaceService usdMarketplaceService))
                return new Error(ErrorCode.MONETIZATION_UNEXPECTED_ERROR);

            if (_currentPortal != ModioAPI.Portal.None && _currentPortal != portal)
                Reset();

            _currentPortal = portal;
                
            // Ensure only one cache update task is running at a time
            _updateSkuCacheTask ??= usdMarketplaceService.UpdateSkuCache();

            Error error;
            (error) = await _updateSkuCacheTask;
            
            _updateSkuCacheTask = null;
            
            _cached = true;
            ApplyPrices();
            
            if (error)
                return error;

            return Error.None;
        }

        static string GetLocalPrice(ModSku sku) => !ModioServices.TryResolve(out IModioUsdMarketplaceService usdMarketplaceService) ? null : usdMarketplaceService.GetLocalPrice(sku);

        public static void TryGetLocalPrice(Mod mod)
        {
            if (mod.PortalSku == null)
                return;
            
            if (_cached)
            {
                mod.FiatPrice = GetLocalPrice(mod.PortalSku);
                mod.InvokeModUpdated(ModChangeType.Everything);

                return;
            }
            ModsWithPrice.Add(mod);
        }

        static void ApplyPrices()
        {
            foreach (Mod mod in ModsWithPrice)
                ApplyPrice(mod);
        }
        
        static void ApplyPrice(Mod mod)
        {
            mod.FiatPrice = GetLocalPrice(mod.PortalSku);
            mod.InvokeModUpdated(ModChangeType.Everything);
        }
    }
}
