using Modio.Platforms;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.Selectables
{
    public class ModioUIInputField : TMP_InputField, IModioUISelectable
    {
        [SerializeField]
        int _layoutPriority = 1; //Note that this isn't exposed in the inspector by default, you must use debug mode

        public override int layoutPriority => _layoutPriority;

        public event IModioUISelectable.SelectableStateChangeDelegate StateChanged;

        public IModioUISelectable.SelectionState State { get; private set; }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);

            if (!ModioServices.TryResolve(out IVirtualKeyboardHandler keyboardHandler)) return;
            
            var virtualKeyboardType = ModioVirtualKeyboardType.Default;
            if (contentType == ContentType.EmailAddress)
                virtualKeyboardType = ModioVirtualKeyboardType.EmailAddress;
                
            keyboardHandler.OpenVirtualKeyboard(null, null, text, virtualKeyboardType, characterLimit, multiLine, s =>
            {
                text = s;

                OnSubmit(null);
                OnDeselect(null);
            });

        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            State = (IModioUISelectable.SelectionState)state;
            StateChanged?.Invoke(State, instant);
        }

        /// <summary>Transitions to a <see cref="Selectable.SelectionState"/> visually, without focusing the <see cref="TMP_InputField"/> or modifying <see cref="EventSystem.currentSelectedGameObject"/>.</summary>
        public void DoVisualOnlyStateTransition(IModioUISelectable.SelectionState state, bool instant) =>
            DoStateTransition((SelectionState)state, instant);
    }
}
