using System;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Scripts.Themes.Options
{
    [Serializable]
    public class TmpFontStyleOption : BaseStyleOption<TMP_Text>
    {
        [SerializeField] FontStyles _fontStyles;
        [SerializeField] TmpSpacingWrapper _spacing;
        
        protected override void StyleComponent(TMP_Text component)
        {
            component.fontStyle = _fontStyles;

            component.characterSpacing = _spacing.CharacterSpacing;
            component.wordSpacing = _spacing.WordSpacing;
            component.lineSpacing = _spacing.LineSpacing;
            component.paragraphSpacing = _spacing.ParagraphSpacing;
        }
    }
}
