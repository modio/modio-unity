using System;
using Modio.Unity.UI.Input;
using UnityEngine;
using UnityEngine.Events;

namespace Modio.Unity.UI.Components.Selectables.Transitions
{
    [Serializable]
    public class SelectableTransitionActionListenerOnSelected : ISelectableTransition, IPropertyMonoBehaviourEvents
    {
        [SerializeField] ModioUIInput.ModioAction _inputAction;

        [SerializeField] UnityEvent _onPressed;

        public void OnSelectionStateChanged(IModioUISelectable.SelectionState state, bool instant)
        {
            if (state == IModioUISelectable.SelectionState.Selected)
            {
                ModioUIInput.AddHandler(_inputAction, ActionPressed);
            }
            else if (state != IModioUISelectable.SelectionState.Pressed)
            {
                ModioUIInput.RemoveHandler(_inputAction, ActionPressed);
            }
        }

        void ActionPressed()
        {
            _onPressed.Invoke();
        }

        public void Start() { }

        public void OnDestroy()
        {
            ModioUIInput.RemoveHandler(_inputAction, ActionPressed);
        }

        public void OnEnable() { }

        public void OnDisable()
        {
            ModioUIInput.RemoveHandler(_inputAction, ActionPressed);
        }
    }
}
