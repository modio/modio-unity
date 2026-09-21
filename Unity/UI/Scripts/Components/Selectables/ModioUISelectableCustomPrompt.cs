using System;
using Modio.Unity.UI.Components.Localization;
using Modio.Unity.UI.Input;
using UnityEngine;

namespace Modio.Unity.UI.Components.Selectables
{
    public class ModioUISelectableCustomPrompt : MonoBehaviour
    {
        [SerializeField] string _localizedInputPrompt;
        [SerializeField] ModioUILocalizedText _pullTextFrom;
        
        IModioUISelectable _owner;

        void Awake()
        {
            _owner = GetComponentInParent<IModioUISelectable>();
        }
        
        void OnEnable()
        {
            if (_owner != null)
            {
                _owner.StateChanged += OnSelectionStateChanged;
                OnSelectionStateChanged(_owner.State, true);
            }
        }

        void OnDisable()
        {
            if (_owner != null) _owner.StateChanged -= OnSelectionStateChanged;
            if (_pullTextFrom != null) _pullTextFrom.OnKeyChanged -= KeyChanged;

            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.DynamicSelect, OnPressed);
        }

        void OnSelectionStateChanged(IModioUISelectable.SelectionState state, bool instant)
        {
            if (string.IsNullOrEmpty(_localizedInputPrompt) && _pullTextFrom == null) return;
            
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.DynamicSelect, OnPressed);

            if (state is IModioUISelectable.SelectionState.Selected or IModioUISelectable.SelectionState.Pressed)
            {
                string locKey = _localizedInputPrompt;

                if (_pullTextFrom != null)
                {
                    locKey = _pullTextFrom.Key;
                    _pullTextFrom.OnKeyChanged -= KeyChanged;
                    _pullTextFrom.OnKeyChanged += KeyChanged;
                }
                
                ModioUIInput.AddHandler(ModioUIInput.ModioAction.DynamicSelect, OnPressed, locKey);
            }
        }

        void KeyChanged(string key)
        {
            OnSelectionStateChanged(_owner.State, true);
        }

        // Does nothing, but we need it to be unique to trick the input system
        void OnPressed()
        {
        }
    }
}
