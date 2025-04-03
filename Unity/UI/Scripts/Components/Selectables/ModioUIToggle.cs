using UnityEngine.UI;

namespace Modio.Unity.UI.Components.Selectables
{
    public class ModioUIToggle : Toggle, IModioUISelectable
    {
        public event IModioUISelectable.SelectableStateChangeDelegate StateChanged;

        public IModioUISelectable.SelectionState State { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            onValueChanged.AddListener(value => DoStateTransition((SelectionState)State, false));
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            State = (IModioUISelectable.SelectionState)state;
            StateChanged?.Invoke(State, instant);
        }

        public void FakeClicked()
        {
            //Replicates the private method InternalToggle()
            if (!IsActive() || !IsInteractable()) return;

            isOn = !isOn;
        }
    }
}
