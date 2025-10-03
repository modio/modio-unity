using System.Collections.Generic;
using System.Threading.Tasks;
using Modio.Authentication;
using Modio.Platforms.Wss;
using Modio.Unity.UI.Scripts.Components;
using Modio.Users;
using UnityEngine;

namespace Modio.Unity.UI.Panels.Authentication
{
    public class ModioAuthenticationPickerPanel : ModioPanelBase
    {
        [SerializeField] ModioUIAuthenticationPickerButton _platformPickerButton;
        [SerializeField] ModioUIAuthenticationPickerButton _emailButton;
        [SerializeField] ModioUIAuthenticationPickerButton _wssButton;

        Dictionary<IModioAuthService, ModioUIAuthenticationPickerButton> _constructedButtons;

        public override void OnGainedFocus(GainedFocusCause selectionBehaviour)
        {
            base.OnGainedFocus(selectionBehaviour);

            // When we finish authenticating, we return to this panel, thus we check if we're authenticated to know
            // when we need this panel to close
            if (User.Current is not null
                && User.Current.IsAuthenticated) 
                ClosePanel();
            
            if (!ModioServices.TryResolve(out ModioMultiplatformAuthResolver authResolver))
                authResolver = new ModioUnityMultiplatformAuthResolver();
            
            _constructedButtons ??= new Dictionary<IModioAuthService, ModioUIAuthenticationPickerButton>();
            
            foreach (IModioAuthService service in authResolver.AuthBindings)
            {
                if (_constructedButtons.TryGetValue(service, out _)) continue;

                switch (service)
                {
                    // These are separate cases as they share the same portal, but require different icons
                    case ModioEmailAuthService:
                        _constructedButtons[service] = _emailButton;
                        _emailButton.gameObject.SetActive(true);
                        _emailButton.SetBoundAuthService(service);
                        continue;
                    case WssAuthService:
                        _constructedButtons[service] = _wssButton;
                        _wssButton.gameObject.SetActive(true);
                        _wssButton.SetBoundAuthService(service);
                        continue;
                    default:
                    {
                        ModioUIAuthenticationPickerButton button = Instantiate(_platformPickerButton, _platformPickerButton.transform.parent);
                        button.SetBoundAuthService(service);
                        _constructedButtons[service] = button;
                        button.gameObject.SetActive(true);
                        break;
                    }
                }
            }
        }

        public void ChooseAuthMethod(IModioAuthService service)
        {
            var resolver = ModioServices.Resolve<ModioMultiplatformAuthResolver>();

            resolver.ServiceOverride = service;
            
            if (service is not IPotentialModioEmailAuthService { IsEmailPlatform: true, }) 
                ModioPanelManager.GetPanelOfType<ModioAuthenticationPanel>()?.AttemptSso(service, false);
            else
                ModioPanelManager.GetPanelOfType<ModioAuthenticationIEmailPanel>()?.OpenPanel();
        }
    }
}
