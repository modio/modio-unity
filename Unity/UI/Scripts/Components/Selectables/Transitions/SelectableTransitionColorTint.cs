using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.Selectables.Transitions
{
    [Serializable]
    public class SelectableTransitionColorTint : ISelectableTransition
    {
        [SerializeField] Graphic _target;
        [SerializeField] ColorBlock _colorBlock = ColorBlock.defaultColorBlock;
        Coroutine _coroutine;

        public void OnSelectionStateChanged(IModioUISelectable.SelectionState state, bool instant)
        {
            if (_target == null) return;

            Color targetColor = state switch
            {
                IModioUISelectable.SelectionState.Normal      => _colorBlock.normalColor,
                IModioUISelectable.SelectionState.Highlighted => _colorBlock.highlightedColor,
                IModioUISelectable.SelectionState.Pressed     => _colorBlock.pressedColor,
                IModioUISelectable.SelectionState.Selected    => _colorBlock.selectedColor,
                IModioUISelectable.SelectionState.Disabled    => _colorBlock.disabledColor,
                _                                             => _colorBlock.normalColor,
            };

            if (_target.gameObject.activeInHierarchy)
            {
                if (_coroutine != null) _target.StopCoroutine(_coroutine);
                _coroutine = _target.StartCoroutine(CrossFadeColor(targetColor, !instant ? _colorBlock.fadeDuration : 0));
            }
            else
            {
                _target.color = targetColor;
            }
        }

        IEnumerator CrossFadeColor(Color targetColor, float duration)
        {
            var startColor = _target.color;

            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / duration)
            {
                _target.color = Color.Lerp(startColor, targetColor, t);

                yield return null;
            }

            _target.color = targetColor;
            _coroutine = null;
        }
    }
}
