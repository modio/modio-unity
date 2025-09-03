using System;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Scripts.Themes.Options
{
    [Serializable]
    public class TmpFontSizeOption : BaseStyleOption<TMP_Text>
    {
        [SerializeField] float _fontSize;
        
        protected override void StyleComponent(TMP_Text component)
        {
            component.fontSize = _fontSize;
        }
    }
}
