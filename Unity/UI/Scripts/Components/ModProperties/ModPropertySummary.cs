using System;
using Modio.Mods;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertySummary : ModioResourceProperty
    {
        [SerializeField] TMP_Text _text;

        [SerializeField] GameObject _enableIfDescriptionDiffers;

        protected override void OnResourceUpdate(IModioInfo resource)
        {
            if (_text != null) _text.text = resource.Summary;

            if (_enableIfDescriptionDiffers != null)
            {
                bool descriptionDiffers = !string.IsNullOrEmpty(resource.Description) && resource.Description != resource.Summary;
                _enableIfDescriptionDiffers.SetActive(descriptionDiffers);
            }
        }
    }
}
