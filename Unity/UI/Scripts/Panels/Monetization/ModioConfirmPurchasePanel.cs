using System.Threading.Tasks;
using Modio.Errors;
using Modio.Mods;
using Modio.Unity.UI.Components;
using UnityEngine;

namespace Modio.Unity.UI.Panels.Monetization
{
    public class ModioConfirmPurchasePanel : ModioPanelBase
    {
        ModioUIMod _modioUIMod;

        [SerializeField] bool _subscribeOnPurchase = true;

        protected override void Awake()
        {
            base.Awake();
            _modioUIMod = GetComponent<ModioUIMod>();
        }

        public void OpenPanel(Mod mod)
        {
            OpenPanel();
            _modioUIMod.SetMod(mod);
        }

        public void ConfirmPurchase()
        {
            ConfirmPurchaseFlow();
        }

        async void ConfirmPurchaseFlow()
        {
            Mod mod = _modioUIMod.Mod;

            Task<Error> purchaseItemTask = mod.Purchase(_subscribeOnPurchase);

            Error error = await ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>()
                                                     .OpenAndWaitForAsync(purchaseItemTask);

            if (error.Code != ErrorCode.NONE)
                ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>().OpenPanel(error);
            else
                ClosePanel();
        }
    }
}
