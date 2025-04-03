using System;
using UnityEngine;

namespace Modio.Unity.UI.Components.Selectables.Transitions
{
    [Serializable]
    public class SelectableTransitionActive : ISelectableTransition
    {
        [SerializeField] GameObject _target;
        [SerializeField] bool _normal;
        [SerializeField] bool _highlighted;
        [SerializeField] bool _pressed;
        [SerializeField] bool _selected;
        [SerializeField] bool _disabled;

        public void OnSelectionStateChanged(IModioUISelectable.SelectionState state, bool instant)
        {
            if (_target == null) return;

            _target.SetActive(
                state switch
                {
                    IModioUISelectable.SelectionState.Normal      => _normal,
                    IModioUISelectable.SelectionState.Highlighted => _highlighted,
                    IModioUISelectable.SelectionState.Pressed     => _pressed,
                    IModioUISelectable.SelectionState.Selected    => _selected,
                    IModioUISelectable.SelectionState.Disabled    => _disabled,
                    _                                             => _normal,
                }
            );
        }
    }
}
