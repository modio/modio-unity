namespace Modio.Unity.UI.Components.Selectables
{
    public interface IModioUISelectable
    {
        public enum SelectionState
        {
            Normal,
            Highlighted,
            Pressed,
            Selected,
            Disabled,
        }

        delegate void SelectableStateChangeDelegate(SelectionState state, bool instant);

        event SelectableStateChangeDelegate StateChanged;

        SelectionState State { get; }
    }
}
