using System.Linq;
using System.Threading.Tasks;
using Modio.API;
using Modio.Authentication;
using Modio.Extensions;
using Modio.Unity.Settings;
using Modio.Users;
using UnityEngine;
using UnityEngine.Events;
using ErrorCode = Modio.Errors.ErrorCode;

namespace Modio.Unity.UI.Panels.Authentication
{
    public class ModioAuthenticationPanel : ModioPanelBase
    {
        [SerializeField] UnityEvent<Error> _onError;
        [SerializeField] UnityEvent<Error> _onOffline;
        
        [ModioDebugMenu(ShowInBrowserMenu = false, ShowInSettingsMenu = true)]
        static bool ForceShowTermsOfUse { get; set; }

        bool _fallbackToEmailAuth;

        protected override void Awake()
        {
            base.Awake();

            ModioClient.OnShutdown += OnPluginShutdown;
        }

        protected override void OnDestroy()
        {
            ModioClient.OnInitialized -= OnPluginReady;
            ModioClient.OnShutdown -= OnPluginShutdown;

            base.OnDestroy();
        }

        /// <summary>
        /// Opens the widget. Will open the correct type of login flow
        /// </summary>
        public void OpenAuthFlow()
        {
            if (User.Current != null && User.Current.IsAuthenticated)
            {
                Debug.LogWarning("Attempted to open Auth Flow when already logged in");
                return;
            }

            if (!ModioClient.IsInitialized)
            {
                var waitingPanel = ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>();
            
                if (waitingPanel != null && !waitingPanel.HasFocus)
                    waitingPanel.OpenPanel();

            }
            
            ModioClient.OnInitialized -= OnPluginReady;
            ModioClient.OnInitialized += OnPluginReady;
        }

        void OnPluginShutdown() => ModioClient.OnInitialized -= OnPluginReady;

        void OnPluginReady()
        {
            if (ModioClient.AuthService != null 
                && ModioClient.AuthService is not IPotentialModioEmailAuthService { IsEmailPlatform: true, }
                && User.Current is not null
                && User.Current.AuthenticatedPortal != ModioAPI.Portal.None
            ) {
                var authService = ModioServices.GetBindings<IModioAuthService>()
                                               .ResolveAll()
                                               .FirstOrDefault(binding => binding.service.Portal ==
                                                                          User.Current.AuthenticatedPortal
                                               )
                                               .service;

                if (authService is not null)
                {
                    OpenPanel();
                    AttemptSso(authService, false).ForgetTaskSafely();
                    return;
                }
            }

            if (ModioUnityMultiplatformAuthResolver.IsSupportedPlatform)
            {
                OpenPanel();
                
                var waitingPanel = ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>();
                
                waitingPanel?.ClosePanel();
                
                ModioPanelManager.GetPanelOfType<ModioAuthenticationPickerPanel>().OpenPanel();
                return;
            }
            
            GetTermsAndShowPanel().ForgetTaskSafely();
        }

        async Task GetTermsAndShowPanel()
        {
            OpenPanel();
            
            var waitingPanel = ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>();
            
            if (waitingPanel != null && !waitingPanel.HasFocus)
                waitingPanel.OpenPanel();

            (Error error, TermsOfUse result) = await TermsOfUse.Get();

            waitingPanel?.ClosePanel();
            
            if (error.Code == ErrorCode.CANNOT_OPEN_CONNECTION)
            {
                _onOffline.Invoke(error);
                return;
            }
            
            // SSO platforms will initialize this before this panel is opened, email will not
            if (ModioClient.AuthService != null 
                && ModioClient.AuthService is not IPotentialModioEmailAuthService { IsEmailPlatform: true, }) {
                ModioPanelManager.GetPanelOfType<ModioAuthenticationTermsOfServicePanel>()?.OpenPanel();
            }
            else
                ModioPanelManager.GetPanelOfType<ModioAuthenticationIEmailPanel>()?.OpenPanel();
        }

        void LateUpdate()
        {
            if (!HasFocus) return;
            if (_fallbackToEmailAuth)
            {
                _fallbackToEmailAuth = false;
                ModioPanelManager.GetPanelOfType<ModioAuthenticationIEmailPanel>()?.OpenPanel();
                return;
            }
            ClosePanel();
        }

        public async Task AttemptSso(IModioAuthService authService, bool agreedToTerms)
        {
            var waitingPanel = ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>();
            waitingPanel?.OpenPanel();
            
            Error error;

            if (ForceShowTermsOfUse && !agreedToTerms)
                error = new Error(ErrorCode.USER_NO_ACCEPT_TERMS_OF_USE);
            else
                error = await authService.Authenticate(agreedToTerms);
            
            if (!error)
            {
                waitingPanel?.ClosePanel();
                ModioLog.Verbose?.Log($"Signed in successfully");
            }
            else if (!agreedToTerms && error.Code == ErrorCode.USER_NO_ACCEPT_TERMS_OF_USE) // 11074
            {
                ModioLog.Message?.Log("User hasn't agreed to terms");

                OpenPanel();

                GetTermsAndShowPanel().ForgetTaskSafely();

                return;
            }
            else
            {
                if (!error.IsSilent) 
                    ModioLog.Error?.Log($"SSO failed: {error.GetMessage()} (agreed to terms {agreedToTerms})");
                _onError?.Invoke(error);
                waitingPanel?.ClosePanel();

                if (error.Code != ErrorCode.CANNOT_OPEN_CONNECTION)
                {
                    var compUISettings = ModioClient.Settings.GetPlatformSettings<ModioComponentUISettings>();
                    if (compUISettings != null && compUISettings.FallbackToEmailAuthentication)
                        _fallbackToEmailAuth = true;                    
                }
            }

            ModioPanelManager.GetPanelOfType<ModioAuthenticationTermsOfServicePanel>()?.ClosePanel();
        }
    }
}
