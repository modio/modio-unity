using System;
using Modio.Mods;
using Modio.Unity.UI.Panels;
using Plugins.Modio.Modio.Ratings;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyRatingsToggles : IModProperty
    {
        [SerializeField] Toggle _positiveVoteToggle;
        [SerializeField] Toggle _negativeVoteToggle;

        Mod _mod;

        public void OnModUpdate(Mod mod)
        {
            _mod = mod;
            
            var ratingResult = mod.CurrentUserRating;

            _positiveVoteToggle.onValueChanged.RemoveListener(PositiveToggleValueChanged);
            _negativeVoteToggle.onValueChanged.RemoveListener(NegativeToggleValueChanged);

           _positiveVoteToggle.isOn = ratingResult == ModioRating.Positive;
            _negativeVoteToggle.isOn = ratingResult == ModioRating.Negative;

            _positiveVoteToggle.onValueChanged.AddListener(PositiveToggleValueChanged);
            _negativeVoteToggle.onValueChanged.AddListener(NegativeToggleValueChanged);
        }

        void PositiveToggleValueChanged(bool arg0)
        {
            var task = _mod.RateMod(_positiveVoteToggle.isOn ? ModioRating.Positive : ModioRating.None);

            ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
        }

        void NegativeToggleValueChanged(bool toggleValue)
        {
            var task = _mod.RateMod(_negativeVoteToggle.isOn ? ModioRating.Negative : ModioRating.None);

            ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
        }
    }
}
