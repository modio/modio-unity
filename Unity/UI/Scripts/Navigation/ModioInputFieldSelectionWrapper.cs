using System.Collections;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Input;
using Modio.Unity.UI.Panels;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modio.Unity.UI.Navigation
{
    /// <summary>
    /// Place on a parent gameObject to a MultiTargetInputField to prevent controller input getting stuck in the input field
    /// Will highlight the input field when selected, and select the underlying input field on submit
    /// </summary>
    public class ModioInputFieldSelectionWrapper : Selectable, ISubmitHandler
    {
        TMP_InputField _inputField;
        LayoutElement _layoutElement;
        bool _isExpanded;

        [SerializeField] bool _keepFocusOnSubmit;
        [SerializeField] bool _animateSelectionWidth;
        [SerializeField] GameObject _disableWhenCollapsed;

        protected override void Awake()
        {
            base.Awake();

            _inputField = GetComponentInChildren<TMP_InputField>();

            var fieldNavigation = _inputField.navigation;
            fieldNavigation.mode = UnityEngine.UI.Navigation.Mode.None;
            _inputField.navigation = fieldNavigation;

            _layoutElement = GetComponent<LayoutElement>();

            _inputField.onSelect.AddListener(
                s =>
                {
                    ModioPanelManager.GetInstance().PushFocusSuppression();

                    ModioUIInput.AddHandler(ModioUIInput.ModioAction.Cancel, OnPressedCancel);

                    UpdateAnimation(true);
                }
            );

            _inputField.onDeselect.AddListener(
                s =>
                {
                    ModioPanelManager.GetInstance()
                                     .PopFocusSuppression(ModioPanelBase.GainedFocusCause.InputSuppressionChangeOnly);

                    ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.Cancel, OnPressedCancel);

                    UpdateAnimation();
                }
            );

            _inputField.onEndEdit.AddListener(OnEndEdit);

            //Listen for when something external clears the text
            _inputField.onValueChanged.AddListener(
                s =>
                {
                    if (string.IsNullOrEmpty(s)) UpdateAnimation();
                }
            );

            if (_disableWhenCollapsed != null) _disableWhenCollapsed.SetActive(false);
        }

        protected override void OnDestroy()
        {
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.Cancel, OnPressedCancel);
            base.OnDestroy();
        }

        void OnEndEdit(string s)
        {
            if (EventSystem.current.currentSelectedGameObject == _inputField.gameObject)
            {
                if (!_keepFocusOnSubmit)
                    ModioPanelManager.GetInstance()
                                     .PopFocusSuppression(
                                         ModioPanelBase.GainedFocusCause.RegainingFocusFromStackedPanel
                                     );
                else if (!EventSystem.current.alreadySelecting) EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        void OnPressedCancel()
        {
            if (EventSystem.current.currentSelectedGameObject == _inputField.gameObject)
            {
                _inputField.OnDeselect(null);
                UpdateSelectedVisuals(true);
            }
        }

        void UpdateAnimation(bool gainingFocus = false)
        {
            if(!_animateSelectionWidth) return;

            var shouldBeExpanded = (_inputField.isFocused || gainingFocus) || _inputField.text.Length > 0;

            if (_isExpanded != shouldBeExpanded)
            {
                _isExpanded = shouldBeExpanded;
                StartCoroutine(Animate(shouldBeExpanded));
            }
        }

        IEnumerator Animate(bool hasFocus)
        {
            var startWidth = _layoutElement.flexibleWidth;
            var targetWidth = hasFocus ? 40 : 0;

            float duration = 0.3f;
            if (_disableWhenCollapsed != null) _disableWhenCollapsed.SetActive(true);

            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / duration)
            {
                var animT = t * t; //ease in

                if (!hasFocus)
                {
                    animT = 1 - (1 - t) * (1 - t); //ease out
                }

                _layoutElement.flexibleWidth = Mathf.Lerp(startWidth, targetWidth, animT);

                yield return null;
            }

            _layoutElement.flexibleWidth = targetWidth;
            if (_disableWhenCollapsed != null) _disableWhenCollapsed.SetActive(hasFocus);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            UpdateSelectedVisuals(true);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            UpdateSelectedVisuals(false);
        }

        void UpdateSelectedVisuals(bool selected)
        {
            if (!(_inputField is ModioUIInputField modioInputField)) return;

            var state = selected ? IModioUISelectable.SelectionState.Selected : IModioUISelectable.SelectionState.Normal;
            modioInputField.DoVisualOnlyStateTransition(state, false);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            SelectInputField();
        }

        public void SelectInputField()
        {
            StartCoroutine(SelectChildDelayed());
        }

        IEnumerator SelectChildDelayed()
        {
            yield return new WaitForEndOfFrame();

            _inputField.Select();
        }
    }
}
