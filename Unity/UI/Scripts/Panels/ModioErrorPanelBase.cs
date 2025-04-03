using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modio.Errors;
using Modio.Unity.UI.Components.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Modio.Unity.UI.Panels
{
    public abstract class ModioErrorPanelBase : ModioPanelBase
    {
        [SerializeField] ModioUILocalizedText _titleLocalised;
        [SerializeField] TMP_Text _errorCode;
        [SerializeField] ModioUILocalizedText _errorCodeLocalised;
        [SerializeField] ModioUILocalizedText _errorMessageLocalised;

        [SerializeField] GameObject _showWhenActionProvided;
        [SerializeField] ModioUILocalizedText _actionMessageLocalised;

        [SerializeField] ErrorMessageResponse[] _errorMessageResponses;

        Action _action;
        bool _useLocalizedActionPrompt;

        [Serializable]
        public class ErrorMessageResponse
        {
            public List<long> errorCode;
            public List<long> apiCode;

            public string windowTitleLocalised;
            public string windowMessageLocalised;

            public string actionPromptLocalised;
            public UnityEvent onActionPressed;
        }

        public void OpenPanel(Error error)
        {
            OpenPanel();

            foreach (var messageResponse in _errorMessageResponses)
            {
                if (messageResponse.errorCode.Contains((long)error.Code) 
                    || (messageResponse.apiCode.Count != 0 && messageResponse.apiCode.Contains((long)error.Code)))
                {
                    if (error is RateLimitError rateLimitError) 
                        OpenPanel(messageResponse, rateLimitError.RetryAfterSeconds);
                    else
                        OpenPanel(messageResponse);
                    
                    return;
                }
            }

            if (_errorCode != null) _errorCode.text = $"[Error code: {error.Code}]";

            if (_errorMessageLocalised != null)
            {
                var errorLocKey = "modio_error_description_api_" + error.Code;
                if (!_errorMessageLocalised.SetKeyIfItExists(errorLocKey))
                {
                    _errorMessageLocalised.ResetKey();
                }
            }

            if (_errorCodeLocalised != null)
            {
                _errorCodeLocalised.gameObject.SetActive(true);
                var responseErrorCode = error.Code.ToString();
                _errorCodeLocalised.SetFormatArgs(responseErrorCode);
            }

            //reset things that may have been overriden by other methods
            if (_showWhenActionProvided != null) _showWhenActionProvided.SetActive(false);
            if (_titleLocalised != null) _titleLocalised.ResetKey();
            _action = null;
            
            ModioLog.Verbose?.Log($"Showing error for response {error}");
        }

        public void OpenPanel(string message)
        {
            OpenPanel();

            _action = null;
            
            if (_showWhenActionProvided != null) _showWhenActionProvided.SetActive(false);
            if (_titleLocalised != null) _titleLocalised.ResetKey();

            if (_errorCode != null) _errorCode.text = message;
        }
        
        public void OpenPanel(ErrorMessageResponse response, params object[] args)
        {
            if(_titleLocalised != null)
                _titleLocalised.SetKey(response.windowTitleLocalised);
            if (_errorMessageLocalised != null) 
                _errorMessageLocalised.SetKey(response.windowMessageLocalised, args);
            if (_actionMessageLocalised != null) 
                _actionMessageLocalised.SetKey(response.actionPromptLocalised);
            
            _useLocalizedActionPrompt = !string.IsNullOrEmpty(response.actionPromptLocalised);
            
            if (_showWhenActionProvided != null) 
                _showWhenActionProvided.SetActive(_useLocalizedActionPrompt);
            if (_errorCodeLocalised != null)
                _errorCodeLocalised.gameObject.SetActive(false);

            _action = response.onActionPressed.Invoke;
        }

        protected override void CancelPressed()
        {
            if (!_useLocalizedActionPrompt)
                _action?.Invoke();
            
            base.CancelPressed();
        }

        public async Task MonitorTaskThenOpenPanelIfError(Task<Error> task)
        {
            try 
            { 
                Error error = await task;

                if (error) 
                    OpenPanel(error);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void InvokeAction()
        {
            if (_action != null) _action.Invoke();
            ClosePanel();
        }
    }
}
