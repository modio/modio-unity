using System;
using System.Linq;
using System.Threading.Tasks;
using Modio.Mods;
using Modio.Unity.UI.Components;
using Modio.Unity.UI.Search;
using UnityEngine.UI;

namespace Modio.Unity.UI.Panels
{
    public class ModSortPanel : ModioPanelBase
    {
        protected override void Awake()
        {
            base.Awake();

            _toggles = GetComponentsInChildren<Toggle>(true);
        }

        Toggle[] _toggles;

        public override void DoDefaultSelection()
        {
            SetSelectedGameObject(
                _toggles.FirstOrDefault(t => t.isOn)?.gameObject ?? _toggles.First().gameObject
            );
        }

        public override void OnGainedFocus(GainedFocusCause selectionBehaviour)
        {
            SortModsBy currentSortBy = ModioUISearch.Default.LastSearchFilter.SortBy;

            foreach (Toggle toggle in _toggles)
            {
                var isCurrentSort = toggle.GetComponent<ModioUISortModsToggle>().SortModsBy == currentSortBy;

                toggle.isOn = isCurrentSort;
            }

            base.OnGainedFocus(selectionBehaviour);
        }

        public async void ApplySort()
        {
            await Task.Yield(); // Temporary fix to allow MultiTargetToggleGroup to apply everything it needs to

            ModioUISortModsToggle selectedToggle =
                _toggles.FirstOrDefault(toggle => toggle.isOn)?.GetComponent<ModioUISortModsToggle>();

            if (selectedToggle == null) return;

            if (selectedToggle.SortModsBy == ModioUISearch.Default.LastSearchFilter.SortBy)
            {
                return;
            }

            //Close before applying sort. This allows a cached search to be applied while the main view has focus
            ClosePanel();

            bool ascending = selectedToggle.SortModsBy switch
            {
                SortModsBy.Name    => true,
                SortModsBy.Price   => false,
                SortModsBy.Rating  => true,
                SortModsBy.Popular => false,
                SortModsBy.Downloads =>
                    true, // Note: this is a mistake on the backend api. Ascending is swapped with descending for this field
                SortModsBy.Subscribers   => true,
                SortModsBy.DateSubmitted => false,
                _                        => throw new ArgumentOutOfRangeException()
            };

            ModioUISearch.Default.ApplySortBy(selectedToggle.SortModsBy, ascending);
        }
    }
}
