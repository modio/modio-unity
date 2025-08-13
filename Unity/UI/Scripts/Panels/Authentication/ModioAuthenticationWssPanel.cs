using System;
using System.Threading.Tasks;
using Modio.Authentication;
using Modio.Platforms.Wss;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Modio.Unity.UI.Panels.Authentication
{
    public class ModioAuthenticationWssPanel : ModioPanelBase, IWssCodeDisplayer
    {

        [SerializeField] TMP_Text _codeDisplay;

        [SerializeField] UnityEvent<Error> _onError;

        Action<string> _codeCallback;
        Func<Task> _cancelCallback;

        protected override void Awake()
        {
            base.Awake();
            ModioServices.Bind<IWssCodeDisplayer>().FromInstance(this);
        }

        public override void OnGainedFocus(GainedFocusCause context) 
        {
            base.OnGainedFocus(context);
        }

        public Task ShowCodePrompt(string code, Func<Task> cancelCallback = null)
        {
            if (_codeDisplay != null) _codeDisplay.text = code;
            _cancelCallback = cancelCallback;
            ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>()?.ClosePanel();
            OpenPanel();
            return Task.CompletedTask;
        }

        public Task HideCodePrompt()
        { 
            ClosePanel();
            return Task.CompletedTask;
        }

        public void OnPressCancel()
        {
            _cancelCallback?.Invoke();
            ClosePanel();
        }


    }
}
