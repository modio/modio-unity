using UnityEngine.UI;

namespace Modio.Unity.UI.Components.Selectables
{
    public class ModioUIButton : Button, IModioUISelectable
    {
        public event IModioUISelectable.SelectableStateChangeDelegate StateChanged;

        public IModioUISelectable.SelectionState State { get; private set; }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            State = (IModioUISelectable.SelectionState)state;
            StateChanged?.Invoke(State, instant);
        }

        //Allow faking a state transition when we want to be in a non-standard state (such as highlighting a button in the background)
        public void DoVisualOnlyStateTransition(IModioUISelectable.SelectionState state, bool instant) =>
            DoStateTransition((SelectionState)state, instant);
    }
}
