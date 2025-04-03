using System;
using Modio.Mods;
using Modio.Unity.UI.Panels;
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

           _positiveVoteToggle.isOn = ratingResult == ModRating.Positive;
            _negativeVoteToggle.isOn = ratingResult == ModRating.Negative;

            _positiveVoteToggle.onValueChanged.AddListener(PositiveToggleValueChanged);
            _negativeVoteToggle.onValueChanged.AddListener(NegativeToggleValueChanged);
        }

        void PositiveToggleValueChanged(bool arg0)
        {
            var task = _mod.RateMod(_positiveVoteToggle.isOn ? ModRating.Positive : ModRating.None);

            ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
        }

        void NegativeToggleValueChanged(bool toggleValue)
        {
            var task = _mod.RateMod(_negativeVoteToggle.isOn ? ModRating.Negative : ModRating.None);

            ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
        }
    }
}
