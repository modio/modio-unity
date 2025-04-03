using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Modio.Unity.UI.Panels.Authentication
{
    public class ModioAuthenticationCodePanel : ModioPanelBase
    {
        [SerializeField] TMP_InputField _codeField;

        [SerializeField] TMP_Text _emailDisplay;

        [SerializeField] UnityEvent<Error> _onError;

        Action<string> _codeCallback;

        public override void OnGainedFocus(GainedFocusCause selectionBehaviour)
        {
            if (selectionBehaviour == GainedFocusCause.OpeningFromClosed)
            {
                _codeField.text = "";
            }

            base.OnGainedFocus(selectionBehaviour);
        }

        public void OpenPanel(string emailFieldText, Action<string> codeCallback)
        {
            if (_emailDisplay != null) _emailDisplay.text = emailFieldText;
            _codeCallback = codeCallback;
            OpenPanel();
        }

        public void OnPressSubmitCode()
        {
            ClosePanel();

            ModioPanelManager.GetPanelOfType<ModioAuthenticationWaitingPanel>().OpenPanel();
            
            _codeCallback.Invoke(_codeField.text);
        }

        protected override void CancelPressed()
        {
            OnPressUseAnotherEmail();
            base.CancelPressed();
        }

        public void OnPressUseAnotherEmail()
        {
            ClosePanel();
            _codeCallback?.Invoke(string.Empty);
            ModioPanelManager.GetPanelOfType<ModioAuthenticationIEmailPanel>()?.OpenPanel();
        }

        public void OnPressCancel()
        {
            ClosePanel();
            _codeCallback.Invoke(string.Empty);
        }
    }
}
