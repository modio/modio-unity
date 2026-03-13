using System;
using System.Threading.Tasks;
using Modio.Collections;
using Modio.Extensions;
using Modio.Mods;
using Modio.Unity.UI.Panels;
using Plugins.Modio.Modio.Ratings;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyRatingsToggles : IModProperty, ICollectionProperty
    {
        [SerializeField] Toggle _positiveVoteToggle;
        [SerializeField] Toggle _negativeVoteToggle;

        Mod _mod;
        ModCollection _modCollection;

        public void OnModUpdate(Mod mod)
        {
            _mod = mod;
            _modCollection = null;
            
            OnRatingUpdated(mod.CurrentUserRating);
        }

        public void OnCollectionUpdate(ModCollection collection)
        {
            _mod = null;
            _modCollection = collection;

            OnRatingUpdated(collection.CurrentUserRating);
        }

        void OnRatingUpdated(ModioRating currentRating)
        {
            bool canBeVotedOn = _mod == null || !_mod.IsMonetized || _mod.IsPurchased;
            _positiveVoteToggle.interactable = canBeVotedOn;
            _negativeVoteToggle.interactable = canBeVotedOn;
            
            _positiveVoteToggle.onValueChanged.RemoveListener(PositiveToggleValueChanged);
            _negativeVoteToggle.onValueChanged.RemoveListener(NegativeToggleValueChanged);

            _positiveVoteToggle.isOn = currentRating == ModioRating.Positive;
            _negativeVoteToggle.isOn = currentRating == ModioRating.Negative;

            _positiveVoteToggle.onValueChanged.AddListener(PositiveToggleValueChanged);
            _negativeVoteToggle.onValueChanged.AddListener(NegativeToggleValueChanged);

            DelayedSetNegativeVoteAllowed().ForgetTaskSafely();
        }

        async Task DelayedSetNegativeVoteAllowed()
        {
            (Error readError, GameData data) = await GameData.GetGameData();

            if(readError) return;
            
            _negativeVoteToggle.gameObject.SetActive(
                (data.CommunityOptions & GameCommunityOptions.AllowNegativeRatings) != 0
            );
        }

        void PositiveToggleValueChanged(bool arg0)
        {
            Task<Error> task;
            if(_mod != null)
                task = _mod.RateMod(_positiveVoteToggle.isOn ? ModioRating.Positive : ModioRating.None);
            else
                task = _modCollection.Rate(_positiveVoteToggle.isOn ? ModioRating.Positive : ModioRating.None);
            
            ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
        }

        void NegativeToggleValueChanged(bool toggleValue)
        {
            Task<Error> task;
            if(_mod != null)
                task = _mod.RateMod(_negativeVoteToggle.isOn ? ModioRating.Negative : ModioRating.None);
            else
                task = _modCollection.Rate(_negativeVoteToggle.isOn ? ModioRating.Negative : ModioRating.None);

            ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
        }
    }
}
