using System;
using Modio.Mods;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyDescription : IModProperty
    {
        [SerializeField] TMP_Text _text;

        public void OnModUpdate(Mod mod) => _text.text = mod.Description;
    }
}
