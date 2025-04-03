using System;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.Selectables.Transitions
{
    [Serializable]
    public class SelectableTransitionSpriteSwap : ISelectableTransition
    {
        [SerializeField] Image _target;
        [SerializeField] SpriteState _spriteState;
        [SerializeField] Sprite _overrideDefault;

        bool _isInitialised;
        Sprite _defaultSprite;

        public void OnSelectionStateChanged(IModioUISelectable.SelectionState state, bool instant)
        {
            if (_target == null) return;

            if (!_isInitialised) _defaultSprite = _target.sprite;
            _isInitialised = true;

            _target.sprite = state switch
            {
                IModioUISelectable.SelectionState.Normal =>
                    _overrideDefault != null ? _overrideDefault : _defaultSprite,
                IModioUISelectable.SelectionState.Highlighted => _spriteState.highlightedSprite,
                IModioUISelectable.SelectionState.Pressed     => _spriteState.pressedSprite,
                IModioUISelectable.SelectionState.Selected    => _spriteState.selectedSprite,
                IModioUISelectable.SelectionState.Disabled    => _spriteState.disabledSprite,
                _                                             => _defaultSprite,
            };
        }
    }
}
