using System;
using Modio.Mods;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyInterimFeaturedCategory : IModProperty
    {
        [SerializeField] GameObject _disableIfSearchOff;
        
        [SerializeField] TextMeshProUGUI _label;

        [SerializeField] string _textNew = "New";
        [SerializeField] string _textPremium = "Top Seller";
        [SerializeField] string _textOther = "Featured";
        
        ModioUISearch _search;

        public void OnModUpdate(Mod mod)
        {
            if (_search == null)
            {
                _search = _disableIfSearchOff.GetComponentInParent<ModioUISearch>();
                if (_search == null) return;
            }

            bool enabled = _search.LastSearchSettingsFrom?.ShowFeaturedReasonInCarousel ?? false;
            
            _disableIfSearchOff.SetActive(enabled);
            
            if(!enabled) return;

            string text = _textOther;

            if (mod.IsMonetized && mod.Stats.Subscribers > 0) 
                text = _textPremium;
            else if ((DateTime.Now - mod.DateLive).TotalDays < 14) 
                text = _textNew;
            
            _label.text = text;
        }
    }
}
