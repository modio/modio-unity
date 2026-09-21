using System;
using Modio.Mods;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyName : ModioResourceProperty
    {
        [SerializeField] TMP_Text _text;

        protected override void OnResourceUpdate(IModioInfo resource)
            => _text.text = resource.Name;
    }
}
