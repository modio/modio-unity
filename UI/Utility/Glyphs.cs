using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static ModIO.Utility;

namespace ModIOBrowser.Implementation
{
    class MessageGlyphUpdate : ISimpleMessage { }

    class Glyphs : SimpleMonoSingleton<Glyphs>
    {                
        private ColorScheme colorScheme;
        public GlyphPlatforms PlatformType { get; internal set; }

        public Color glyphColorFallback;
        public Sprite fallbackSprite;
        public Color fallbackColor = Color.white;

        private void Start()
        {
            colorScheme = Browser.Instance.colorScheme;
            ChangeGlyphs(Browser.Instance.uiConfig.GlyphPlatform);
        }

        public Color GetColor(ColorSetterType colorSetter)
        {            
            Color color = colorScheme.GetSchemeColor(colorSetter);
            return color == default(Color) ? fallbackColor : color;
        }

        public void ChangeGlyphs(GlyphPlatforms platform)
        {
            PlatformType = platform;
            SimpleMessageHub.Instance.Publish(new MessageGlyphUpdate());
        }

        [ExposeMethodInEditor] public void ChangeToPc() => ChangeGlyphs(GlyphPlatforms.PC);
        [ExposeMethodInEditor] public void ChangeToXbox() => ChangeGlyphs(GlyphPlatforms.XBOX);
        [ExposeMethodInEditor] public void ChangeToNintendoSwitch() => ChangeGlyphs(GlyphPlatforms.NINTENDO_SWITCH);
        [ExposeMethodInEditor] public void ChangeToPs4() => ChangeGlyphs(GlyphPlatforms.PLAYSTATION_4);
        [ExposeMethodInEditor] public void ChangeToPs5() => ChangeGlyphs(GlyphPlatforms.PLAYSTATION_5);
    }
}
