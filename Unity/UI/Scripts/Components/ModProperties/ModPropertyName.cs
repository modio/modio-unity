using Modio.Mods;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    public class ModPropertyName : IModProperty
    {
        [SerializeField] TMP_Text _text;

        public void OnModUpdate(Mod mod) => _text.text = mod.Name;
    }
}
