using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Scripts.Themes.Options
{
    [Serializable]
    public class TmpFontOption : BaseStyleOption<TMP_Text>
    {
        [SerializeField] TMP_FontAsset _fontAsset;

        protected override void StyleComponent(TMP_Text component)
        {
            component.font = _fontAsset;
        }
    }
}
