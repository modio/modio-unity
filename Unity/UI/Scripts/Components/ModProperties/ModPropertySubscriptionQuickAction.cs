using System;
using System.Threading.Tasks;
using Modio.Mods;
using Modio.Monetization;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Input;
using Modio.Unity.UI.Panels;
using Modio.Unity.UI.Panels.Monetization;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertySubscriptionQuickAction : IModProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] ModioUIButton _selectable;
        Mod _mod;
        bool _hasRegisteredHandler;

        public void OnModUpdate(Mod mod)
        {
            _mod = mod;

            UpdateHooks();
        }

        public void Start()
        {
        }

        public void OnDestroy()
        {
        }

        public void OnEnable()
        {
            _selectable.StateChanged += OnStateChanged;
        }

        public void OnDisable()
        {
            _selectable.StateChanged -= OnStateChanged;
            if (_hasRegisteredHandler) ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.ContextualFastAction, OnPressed);
            _hasRegisteredHandler = false;
        }

        void OnStateChanged(IModioUISelectable.SelectionState state, bool instant)
        {
            UpdateHooks();
        }

        void UpdateHooks()
        {
            if (_hasRegisteredHandler) ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.ContextualFastAction, OnPressed);
            _hasRegisteredHandler = false;

            if(_mod == null) return;

            if (_selectable.State is IModioUISelectable.SelectionState.Selected
                                     or IModioUISelectable.SelectionState.Pressed
                && _selectable.isActiveAndEnabled)
            {
                if (_mod.IsMonetized && !_mod.IsPurchased)
                {
                    AddHandler("modio_btn_purchasemod");
                }
                else if (_mod.IsSubscribed)
                {
                    AddHandler("modio_btn_unsubscribe");
                }
                else
                    AddHandler("modio_btn_subscribe");
            }

            void AddHandler(string localizedKey)
            {
                _hasRegisteredHandler = true;
                ModioUIInput.AddHandler(ModioUIInput.ModioAction.ContextualFastAction, OnPressed, localizedKey);
            }
        }

        void OnPressed()
        {
            if (_mod.IsMonetized && !_mod.IsPurchased)
            {
                var settings = ModioServices.Resolve<ModioSettings>();

                if (!settings.TryGetPlatformSettings(out MonetizationSettings monetizationSettings)) return;
                
                if (monetizationSettings.MonetizationType == ModioMonetizationType.VirtualCurrency)
                    ModioPanelManager.GetPanelOfType<ModioConfirmPurchasePanel>().OpenPanel(_mod);
                else
                    _ = ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>()
                                         .OpenAndWaitForAsync(_mod?.Purchase(true));
            }
            else if (!_mod.IsSubscribed)
            {
                if (_mod.Dependencies.HasDependencies)
                {
                    var modDependenciesPanel = ModioPanelManager.GetPanelOfType<ModDependenciesPanel>();

                    if (modDependenciesPanel != null)
                    {
                        modDependenciesPanel.IsSubscribeFlow(true);
                        modDependenciesPanel.OpenPanel(_mod);
                        return;
                    }
                }
                
                Task<Error> task =_mod.Subscribe();
                ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
            }
            else
            {
                Task<Error> task = _mod.Unsubscribe();
                ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
            }
        }
    }
}
