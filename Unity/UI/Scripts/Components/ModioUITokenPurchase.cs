using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modio.Extensions;
using Modio.Monetization;
using Modio.Users;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUITokenPurchase : MonoBehaviour
    {
        [SerializeField] ModioUITokenPack _referencePack;

        long _cachedGameId;
        readonly List<ModioUITokenPack> _currentPacks = new List<ModioUITokenPack>();

        void Start()
        {
            _referencePack.gameObject.SetActive(false);
            ModioClient.OnInitialized += OnModClientInitialized;
        }

        void OnModClientInitialized()
        {
            // If the game ID has changed, we need to clear the token packs
            if(ModioClient.Settings.GameId != _cachedGameId)
                ClearTokenPacks();

            User.OnUserAuthenticated -= BeginGetCurrencyPacks;
            User.OnUserAuthenticated += BeginGetCurrencyPacks;
        }

        void OnDestroy()
        {
            ModioClient.OnInitialized -= OnModClientInitialized;
            User.OnUserAuthenticated -= BeginGetCurrencyPacks;
            // If we are destroyed, remove the callbacks to prevent exceptions
        }
        
        void BeginGetCurrencyPacks()
        {
            // Return early if we already have packs loaded
            if (_currentPacks.Count > 0) return;
            
            // Fire off the async task to get the currency packs
            GetCurrencyPacks().ForgetTaskSafely();
            
            // Remove the callback, so we don't try to get packs again
            User.OnUserAuthenticated -= BeginGetCurrencyPacks;
        }

        async Task GetCurrencyPacks()
        {
            _cachedGameId = ModioClient.Settings.GameId;
            // Return early if we already have packs loaded
            if (_currentPacks.Count > 0) return;

            if (!ModioServices.TryResolve(out IModioVirtualCurrencyProviderService skuProvider)) return;

            (Error error, PortalSku[] skus) = await skuProvider.GetCurrencyPackSkus();

            if (error) ModioLog.Error?.Log(error);

            ShowTokenPacks(skus);
        }

        void ShowTokenPacks(PortalSku[] sku)
        {
            // Make sure we don't add packs if we already have them loaded
            ClearTokenPacks();

            foreach (PortalSku tokenPack in sku)
            {
                ModioUITokenPack packUI;

                if (_currentPacks.Count > 0)
                {
                    packUI = Instantiate(_referencePack, _referencePack.transform.parent);
                }
                else
                {
                    packUI = _referencePack;
                    _referencePack.gameObject.SetActive(true);
                }

                packUI.SetPack(tokenPack);
                _currentPacks.Add(packUI);
            }

            if (_currentPacks.Count == 0)
            {
                Debug.LogError(
                    "Unable to find any token packs for the current platform. They must be setup on the Game Admin Settings "
                );

                _referencePack.gameObject.SetActive(false);
            }
        }

        void ClearTokenPacks()
        {
            foreach (ModioUITokenPack pack in _currentPacks.Where(pack => pack != _referencePack))
                Destroy(pack.gameObject);

            _currentPacks.Clear();
        }
    }
}
