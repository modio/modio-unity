using System;
using Modio.Mods;
using Modio.Unity.UI.Components.Localization;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public abstract class ModPropertyDateBase : IModProperty
    {
        [SerializeField] TMP_Text _text;
        [SerializeField] string _format = "dd MMM, yy";

        public virtual void OnModUpdate(Mod mod) =>
            _text.text = GetValue(mod).ToString(_format, ModioUILocalizationManager.CultureInfo);

        protected abstract DateTime GetValue(Mod mod);
    }
}
