namespace Modio.Unity.UI.Components.Selectables.Transitions
{
    public interface ISelectableTransition
    {
        void OnSelectionStateChanged(IModioUISelectable.SelectionState state, bool instant);
    }
}
