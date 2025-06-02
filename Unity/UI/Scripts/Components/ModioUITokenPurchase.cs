using System.Collections.Generic;
using System.Threading.Tasks;
using Modio.Extensions;
using Modio.Monetization;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUITokenPurchase : MonoBehaviour
    {
        [SerializeField]
        ModioUITokenPack _referencePack;

        readonly List<ModioUITokenPack> _currentPacks = new List<ModioUITokenPack>();

        void Start()
        {
            _referencePack.gameObject.SetActive(false);

            ModioClient.OnInitialized += OnPluginInitialized;
        }

        void OnDestroy() => ModioClient.OnInitialized -= OnPluginInitialized;

        void OnPluginInitialized() => GetCurrencyPacks().ForgetTaskSafely();

        async Task GetCurrencyPacks()
        {
            if (!ModioServices.TryResolve(out IModioVirtualCurrencyProviderService skuProvider)) 
                return;

            (Error error, PortalSku[] skus) = await skuProvider.GetCurrencyPackSkus();

            if (error) ModioLog.Error?.Log(error);
            
            ShowTokenPacks(skus);
        }

        void ShowTokenPacks(PortalSku[] sku)
        {
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
                Debug.LogError("Unable to find any token packs for the current platform. They must be setup on the Game Admin Settings ");
                _referencePack.gameObject.SetActive(false);
            }
        }
    }
}
