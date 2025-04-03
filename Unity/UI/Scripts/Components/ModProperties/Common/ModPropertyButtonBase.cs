using System;
using Modio.Mods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public abstract class ModPropertyButtonBase<T> : IModProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] Button _button;
        [SerializeField, Tooltip("If true, button presses are ignored while the Component or GameObject are disabled.")]
        bool _ignoreWhileDisabled = true;
        [Space][SerializeField] UnityEvent<T> _onClick;

        Mod _mod;
        bool _addedListener;

        public void OnModUpdate(Mod mod) => _mod = mod;

        public void Start() { }

        public void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnButtonClick);
        }

        public void OnEnable()
        {
            if ((_ignoreWhileDisabled || !_addedListener) && _button != null)
                _button.onClick.AddListener(OnButtonClick);

            _addedListener = true;
        }

        public void OnDisable()
        {
            if (_ignoreWhileDisabled && _button != null) _button.onClick.RemoveListener(OnButtonClick);
        }

        protected void OnButtonClick()
        {
            if (_mod != null) _onClick?.Invoke(GetProperty(_mod));
        }

        protected abstract T GetProperty(Mod mod);
    }
}
