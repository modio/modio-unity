using System;
using Modio.Mods;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public abstract class ModPropertyNumberBase : IModProperty
    {
        [SerializeField] TMP_Text _text;
        [SerializeField, Tooltip(StringFormat.KILO_FORMAT_TOOLTIP)]
        StringFormatKilo _format = StringFormatKilo.Comma;
        [SerializeField, ShowIf(nameof(IsCustomFormat))]
        string _customFormat;

        public void OnModUpdate(Mod mod)
        {
            var value = GetValue(mod);
            if (_text != null) _text.text = StringFormat.Kilo(_format, value, _customFormat);
        }

        protected abstract long GetValue(Mod mod);

        bool IsCustomFormat() => _format == StringFormatKilo.Custom;
    }
}
