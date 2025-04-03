using Modio.Unity.UI.Components;
using Modio.Unity.UI.Input;
using UnityEngine;

namespace Modio.Unity.UI.Panels
{
    public class ModFilterPanel : ModioPanelBase
    {
        ModioUIFilterDisplay _filterDisplay;

        protected override void Awake()
        {
            _filterDisplay = GetComponentInChildren<ModioUIFilterDisplay>();

            base.Awake();
        }

        protected override void CancelPressed()
        {
            //Close before applying filter. This allows a cached search to be applied while the main view has focus
            ClosePanel();

            _filterDisplay.ApplyFilter();
        }

        public override void DoDefaultSelection()
        {
            GameObject selectFilter = _filterDisplay.GetDefaultSelection();
            if (selectFilter)
            {
                SetSelectedGameObject(selectFilter);
                return;
            }

            base.DoDefaultSelection();
        }

        public override void OnGainedFocus(GainedFocusCause selectionBehaviour)
        {
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.Filter,      CancelPressed);
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.FilterClear, _filterDisplay.ClearFilter);

            base.OnGainedFocus(selectionBehaviour);
        }

        public override void OnLostFocus()
        {
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.Filter,      CancelPressed);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.FilterClear, _filterDisplay.ClearFilter);

            base.OnLostFocus();
        }
    }
}
