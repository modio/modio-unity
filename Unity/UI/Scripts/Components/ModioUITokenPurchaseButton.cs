using System.Threading.Tasks;
using Modio.Monetization;
using Modio.Unity.UI.Input;
using Modio.Unity.UI.Panels;
using Modio.Unity.UI.Panels.Monetization;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    /// <summary>
    /// Allows the token button, which is present on multiple panels, to function in a shared way
    /// Will respect the focus of a parent panel if there is one
    /// </summary>
    public class ModioUITokenPurchaseButton : MonoBehaviour
    {
        ModioPanelBase _panel;

        void Awake()
        {
            _panel = GetComponentInParent<ModioPanelBase>();
        }

        void OnEnable()
        {
            if (_panel != null)
                _panel.OnHasFocusChanged += OnHasFocusChanged;
            else
                OnHasFocusChanged(true);
        }

        void OnDisable()
        {
            if (_panel != null) _panel.OnHasFocusChanged -= OnHasFocusChanged;
            OnHasFocusChanged(false);
        }

        void OnHasFocusChanged(bool panelHasFocus)
        {
            //Always remove first to be safe
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.BuyTokens, OpenTokens);

            if (panelHasFocus) ModioUIInput.AddHandler(ModioUIInput.ModioAction.BuyTokens, OpenTokens);
        }

        void OpenTokens()
        {
            if (ModioClient.AuthService == null)
            {
                ModioLog.Error?.Log($"Active platform is null");
                return;
            }
            
            //Implement below when we have a platform that needs it
            if (ModioServices.TryResolve(out IModioVirtualCurrencyProviderService vcProvider))
            {
                var modioBuyTokensPanel = ModioPanelManager.GetPanelOfType<ModioBuyTokensPanel>();
                if (modioBuyTokensPanel != null)
                {
                    modioBuyTokensPanel.OpenPanel();
                    return;
                }
            }

            if (!ModioServices.TryResolve(out IModioStorefrontService purchasePlatform))
            {
                ModioLog.Error?.Log($"No {nameof(IModioStorefrontService)} found, unable to open store front.");
                return;
            }
            
            Task<Error> platformPurchaseFlowTask = purchasePlatform.OpenPlatformPurchaseFlow();

            if (platformPurchaseFlowTask != null)
                ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>()
                                 ?.OpenAndWaitFor(platformPurchaseFlowTask, PlatformPurchaseFlowCompleted);
        }

        void PlatformPurchaseFlowCompleted(Error error)
        {
            if (error) ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.OpenPanel(error);
        }
    }
}
