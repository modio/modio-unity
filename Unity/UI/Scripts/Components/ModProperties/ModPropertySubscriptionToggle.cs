using System;
using Modio.Mods;
using Modio.Unity.UI.Components.Localization;
using Modio.Unity.UI.Panels;
using Modio.Unity.UI.Panels.Monetization;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable, MovedFrom(true, sourceClassName: "ModPropertySubscribePurchaseButtons")]
    public class ModPropertySubscriptionToggle : IModProperty
    {
        [SerializeField] Button _subscribeButton;
        [SerializeField] Toggle _subscribeToggle;
        [SerializeField] Button _unsubscribeButton;

        [SerializeField] Button _purchaseButton;

        [SerializeField] TMP_Text _text;
        [SerializeField] ModioUILocalizedText _localisedText;

        [SerializeField] bool _dependenciesAreConfirmed;

        Mod _mod;

        public void OnModUpdate(Mod mod)
        {
            _mod = mod;

            if (_text != null) _text.text = mod.IsSubscribed ? "UNSUBSCRIBE" : "SUBSCRIBE";

            if (_localisedText != null)
                _localisedText.SetKey(
                    mod.IsSubscribed ? ModioUILocalizationKeys.Btn_Unsubscribe : ModioUILocalizationKeys.Btn_Subscribe
                );

            var availableForPurchase = mod.IsMonetized && !mod.IsPurchased;

            if (_purchaseButton != null)
            {
                _purchaseButton.onClick.RemoveListener(PurchaseButtonClicked);
                _purchaseButton.gameObject.SetActive(availableForPurchase);
                _purchaseButton.onClick.AddListener(PurchaseButtonClicked);
            }

            if (_subscribeToggle != null)
            {
                _subscribeToggle.onValueChanged.RemoveListener(SubscribeToggleValueChanged);

                _subscribeToggle.isOn = mod.IsSubscribed;

                _subscribeToggle.onValueChanged.AddListener(SubscribeToggleValueChanged);

                _subscribeToggle.gameObject.SetActive(!availableForPurchase);
            }

            if (_subscribeButton != null)
            {
                _subscribeButton.onClick.RemoveListener(SubscribeButtonClicked);
                _subscribeButton.onClick.AddListener(SubscribeButtonClicked);

                _subscribeButton.gameObject.SetActive(
                    !availableForPurchase && (_unsubscribeButton == null || !_mod.IsSubscribed)
                );
            }

            if (_unsubscribeButton != null)
            {
                _unsubscribeButton.onClick.RemoveListener(SubscribeButtonClicked);
                _unsubscribeButton.onClick.AddListener(SubscribeButtonClicked);

                _unsubscribeButton.gameObject.SetActive(!availableForPurchase && _mod.IsSubscribed);
            }
        }

        void SubscribeButtonClicked()
        {
            UpdateSubscribed(!_mod.IsSubscribed);
        }

        void SubscribeToggleValueChanged(bool arg0)
        {
            UpdateSubscribed(_subscribeToggle.isOn);
        }

        void UpdateSubscribed(bool shouldBeSubscribed)
        {
            if (shouldBeSubscribed && _mod.Dependencies.HasDependencies)
            {
                if (_dependenciesAreConfirmed)
                {
                    var subWithDependenciesTask = _mod.Subscribe();

                    ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()
                                     ?.MonitorTaskThenOpenPanelIfError(subWithDependenciesTask);

                    if (_subscribeToggle != null) _subscribeToggle.SetIsOnWithoutNotify(_mod.IsSubscribed);
                    return;
                }

                var modDependenciesPanel = ModioPanelManager.GetPanelOfType<ModDependenciesPanel>();

                if (modDependenciesPanel != null)
                {
                    modDependenciesPanel.IsSubscribeFlow(true);
                    modDependenciesPanel.OpenPanel(_mod);

                    if (_subscribeToggle != null) _subscribeToggle.SetIsOnWithoutNotify(_mod.IsSubscribed);
                    return;
                }
            }

            var task = shouldBeSubscribed ? _mod.Subscribe(true) : _mod.Unsubscribe();

            if (_subscribeToggle != null) _subscribeToggle.SetIsOnWithoutNotify(_mod.IsSubscribed);

            ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
        }

        void PurchaseButtonClicked()
        {
            ModioPanelManager.GetPanelOfType<ModioConfirmPurchasePanel>().OpenPanel(_mod);
        }
    }
}
