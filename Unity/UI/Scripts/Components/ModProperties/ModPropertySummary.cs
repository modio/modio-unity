using System;
using Modio.Mods;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertySummary : IModProperty
    {
        [SerializeField] TMP_Text _text;

        [SerializeField] GameObject _enableIfDescriptionDiffers;

        public void OnModUpdate(Mod mod)
        {
            _text.text = mod.Summary;

            if (_enableIfDescriptionDiffers != null)
            {
                bool descriptionDiffers = !string.IsNullOrEmpty(mod.Description) && mod.Description != mod.Summary;
                _enableIfDescriptionDiffers.SetActive(descriptionDiffers);
            }
        }
    }
}
