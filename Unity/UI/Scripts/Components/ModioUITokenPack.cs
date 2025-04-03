using System;
using System.Threading.Tasks;
using Modio.Errors;
using Modio.Monetization;
using Modio.Unity.UI.Panels;
using Modio.Unity.UI.Panels.Monetization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components
{
    public class ModioUITokenPack : MonoBehaviour
    {
        [SerializeField] TMP_Text _amount;
        [SerializeField] TMP_Text _price;
        [SerializeField] TMP_Text _name;
        [SerializeField] Image _icon;

        PortalSku _tokenPack;

        [SerializeField]
        ValueImageMap[] _valuesToImages;

        public void SetPack(PortalSku sku)
        {
            _tokenPack = sku;

            if (_amount != null)
                _amount.text = _tokenPack.Value.ToString();
            if (_price != null)
               _price.text = sku.FormattedPrice;
            if (_name != null)
                _name.text = sku.Name;

            if (_icon != null)
                _icon.sprite = GetImageForValue(sku.Value);
        }

        public void OnPressedPurchase()
        {
            if (!ModioServices.TryResolve(out IModioVirtualCurrencyProviderService skuProvider))
            {
                ModioPanelManager.GetPanelOfType<ModioBuyTokensPanel>().ClosePanel();
                return;
            }

            Task<Error> platformPurchaseFlowTask = skuProvider.OpenCheckoutFlow(_tokenPack);

            if (platformPurchaseFlowTask != null)
                ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>()
                                 ?.OpenAndWaitFor(
                                     platformPurchaseFlowTask,
                                     error =>
                                     {
                                         if (error)
                                         {
                                             if (error.Code == ErrorCode.OPERATION_CANCELLED) return;

                                             ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()
                                                              ?.OpenPanel(error);
                                         }

                                         ModioPanelManager.GetPanelOfType<ModioBuyTokensPanel>()
                                                          ?.ClosePanel();
                                     }
                                 );
        }

        Sprite GetImageForValue(int amount)
        {
            foreach (ValueImageMap map in _valuesToImages)
                if (map.value == amount)
                    return map.image;

            ModioLog.Warning?.Log($"No image mapped for token pack value [{amount}]!");
            return default(Sprite);
        }

        [Serializable]
        class ValueImageMap
        {
            public int value;
            public Sprite image;
        }
    }
}
