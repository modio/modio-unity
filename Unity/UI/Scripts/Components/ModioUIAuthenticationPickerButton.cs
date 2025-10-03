using System;
using Modio.API;
using Modio.Authentication;
using Modio.Unity.UI.Components.Localization;
using Modio.Unity.UI.Panels;
using Modio.Unity.UI.Panels.Authentication;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Scripts.Components
{
    public class ModioUIAuthenticationPickerButton : MonoBehaviour
    {
        [SerializeField] AuthPortalIcon[] _portalIcons;
        [SerializeField] Image _authPortalIcon;
        [SerializeField] TMP_Text _text;
        [SerializeField] ModioUILocalizedText _localization;

        IModioAuthService _authService;

        public void SetBoundAuthService(IModioAuthService authService)
        {
            _authService = authService;
            
            if (!TryGetConfigFromPortal(authService.Portal, out AuthPortalIcon authConfig)) 
                return;

            _localization.SetKey(string.Empty);
            _authPortalIcon.sprite = authConfig.Icon;
            _text.text = authConfig.Name;
        }

        public void ChooseBoundAuthService() 
            => ModioPanelManager.GetPanelOfType<ModioAuthenticationPickerPanel>().ChooseAuthMethod(_authService);

        bool TryGetConfigFromPortal(ModioAPI.Portal portal, out AuthPortalIcon config)
        {
            foreach (AuthPortalIcon portalIcon in _portalIcons)
            {
                if (portalIcon.Portal == portal)
                {
                    config = portalIcon;
                    return true;
                }
            }
            
            config = null;
            return false;
        }

        [Serializable]
        class AuthPortalIcon
        {
            public ModioAPI.Portal Portal;
            public Sprite Icon;
            public string Name;
        }
    }
}
