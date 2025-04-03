using System;
using Modio.Mods;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyVersion : IModProperty
    {
        [SerializeField] TMP_Text _text;
        [SerializeField] GameObject _disableIfNoVersionInfo;

        public void OnModUpdate(Mod mod)
        {
            if (_disableIfNoVersionInfo != null) _disableIfNoVersionInfo.SetActive(!string.IsNullOrEmpty(mod.File.Version));
            if (_text != null) _text.text = mod.File.Version;
        }
    }
}
