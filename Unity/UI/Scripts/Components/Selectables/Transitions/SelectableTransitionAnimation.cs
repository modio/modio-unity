using System;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.Selectables.Transitions
{
    [Serializable]
    public class SelectableTransitionAnimation : ISelectableTransition
    {
        [SerializeField] Animator _target;
        [SerializeField] AnimationTriggers _animationTriggers;

        public void OnSelectionStateChanged(IModioUISelectable.SelectionState state, bool instant)
        {
            if (_target == null || !_target.isActiveAndEnabled || !_target.hasBoundPlayables) return;

            string triggerName = state switch
            {
                IModioUISelectable.SelectionState.Normal      => _animationTriggers.normalTrigger,
                IModioUISelectable.SelectionState.Highlighted => _animationTriggers.highlightedTrigger,
                IModioUISelectable.SelectionState.Pressed     => _animationTriggers.pressedTrigger,
                IModioUISelectable.SelectionState.Selected    => _animationTriggers.selectedTrigger,
                IModioUISelectable.SelectionState.Disabled    => _animationTriggers.disabledTrigger,
                _                                             => null,
            };

            if (string.IsNullOrEmpty(triggerName)) return;

            _target.ResetTrigger(_animationTriggers.normalTrigger);
            _target.ResetTrigger(_animationTriggers.highlightedTrigger);
            _target.ResetTrigger(_animationTriggers.pressedTrigger);
            _target.ResetTrigger(_animationTriggers.selectedTrigger);
            _target.ResetTrigger(_animationTriggers.disabledTrigger);

            _target.SetTrigger(triggerName);
        }
    }
}
