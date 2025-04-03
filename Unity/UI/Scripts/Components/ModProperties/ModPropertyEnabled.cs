using System;
using Modio.Mods;
using Modio.Unity.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyEnabled : IModProperty
    {
        [SerializeField] Toggle _enabledToggle;

        [SerializeField] Button _enableButton;
        [SerializeField] Button _disableButton;

        [SerializeField] GameObject _showIfInstalledWhenEnabledNotAvailable;

        Mod _mod;

        public void OnModUpdate(Mod mod)
        {
            _mod = mod;

            var showEnabledOption = mod.File.State == ModFileState.Installed
                                    && mod.IsSubscribed;

            var compUISettings = ModioClient.Settings.GetPlatformSettings<ModioComponentUISettings>();

            if (compUISettings == null || !compUISettings.ShowEnableModToggle)
            {
                if (_showIfInstalledWhenEnabledNotAvailable != null)
                    _showIfInstalledWhenEnabledNotAvailable.SetActive(showEnabledOption);

                showEnabledOption = false;
            }
            else if (_showIfInstalledWhenEnabledNotAvailable != null)
            {
                _showIfInstalledWhenEnabledNotAvailable.SetActive(false);
            }

            if (_enabledToggle != null)
            {
                _enabledToggle.gameObject.SetActive(showEnabledOption);

                _enabledToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
                _enabledToggle.isOn = mod.IsEnabled;
                _enabledToggle.onValueChanged.AddListener(OnToggleValueChanged);
            }

            if (_enableButton != null)
            {
                _enableButton.onClick.RemoveListener(EnableButtonClicked);
                _enableButton.onClick.AddListener(EnableButtonClicked);

                _enableButton.gameObject.SetActive(!_mod.IsEnabled && showEnabledOption);
            }

            if (_disableButton != null)
            {
                _disableButton.onClick.RemoveListener(DisableButtonClicked);
                _disableButton.onClick.AddListener(DisableButtonClicked);

                _disableButton.gameObject.SetActive(_mod.IsEnabled && showEnabledOption);
            }
        }

        void OnToggleValueChanged(bool isEnabled)
        {
            _mod.SetIsEnabled(isEnabled);
        }

        void EnableButtonClicked()
        {
            OnToggleValueChanged(true);
        }

        void DisableButtonClicked()
        {
            OnToggleValueChanged(false);
        }
    }
}
