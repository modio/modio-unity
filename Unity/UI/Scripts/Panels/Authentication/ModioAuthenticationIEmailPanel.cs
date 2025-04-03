using System.Threading.Tasks;
using Modio.Authentication;
using Modio.Errors;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Modio.Unity.UI.Panels.Authentication
{
    public class ModioAuthenticationIEmailPanel : ModioPanelBase, IEmailCodePrompter
    {
        [SerializeField] TMP_InputField _emailField;

        [SerializeField] UnityEvent<Error> _onError;

        ModioEmailAuthService _authService;
        
        bool _isCodeEntered = false;
        string _authCode = string.Empty;
        

        public override void OnGainedFocus(GainedFocusCause context) 
        {
            base.OnGainedFocus(context);

            _authService = ModioServices.Resolve<ModioEmailAuthService>();
            _authService.SetCodePrompter(this);
        }

        /// <summary>
        /// Invoked when the user submits their email. Requests an authentication email
        /// waits with the loading panel, then opens the resulting panel
        /// </summary>
        public async void OnPressSubmitEmail()
        {
            ClosePanel();

            ModioPanelManager.GetPanelOfType<ModioAuthenticationWaitingPanel>().OpenPanel();
            
            await AuthenticationRequest(_emailField.text, _authService.Authenticate(true, _emailField.text));
        }

        public async void OnPressIHaveCode()
        {
            ClosePanel();

            await AuthenticationRequest(_emailField.text, _authService.AuthenticateWithoutEmailRequest());
        }

        void OnCodeEntered(string code) {
            _authCode = code;
            _isCodeEntered = true;
        }

        async Task AuthenticationRequest(string email, Task<Error> authMethod) 
        {
            Error error = await authMethod;
            
            ModioPanelManager.GetPanelOfType<ModioAuthenticationWaitingPanel>()?.ClosePanel();
            
            if (error)
            {
                if (error.Code == ErrorCode.OPERATION_CANCELLED)
                {
                    ModioLog.Verbose?.Log($"Cancelling Email Authentication Request.");
                    return;
                }

                if (error.Code == ErrorCode.VALIDATION_ERRORS) 
                    error = new Error(ErrorCode.EMAIL_LOGIN_CODE_INVALID);

                ModioLog.Error?.Log($"Error authenticating with email: {error.GetMessage()}\nEmail: {email}");
                _onError.Invoke(error);
                return;
            }
        }

        public async Task<string> ShowCodePrompt() {
            _isCodeEntered = false;
            _authCode = string.Empty;
            
            ModioPanelManager.GetPanelOfType<ModioAuthenticationWaitingPanel>()?.ClosePanel();
            
            ModioPanelManager.GetPanelOfType<ModioAuthenticationCodePanel>()?.OpenPanel(_emailField.text, OnCodeEntered);
            
            while (!_isCodeEntered) {
                await Task.Delay(1000);
            }
            
            ModioLog.Verbose?.Log($"Code entered: {_authCode}");
            
            return _authCode;
        }
    }
}
