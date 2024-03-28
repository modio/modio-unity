using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ModIO.Util;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    class MessageGlyphUpdate : ISimpleMessage { }

    class Glyphs : SelfInstancingMonoSingleton<Glyphs>
    {
        private ColorScheme colorScheme;
        public GlyphPlatforms PlatformType { get; internal set; }

        public Color glyphColorFallback;
        public Sprite fallbackSprite;
        public Color fallbackColor = Color.white;

        private bool hasStarted = false;
        
        private void Start()
        {
            colorScheme = SharedUi.colorScheme;
            if(this.PlatformType == default)
                ChangeGlyphs(SharedUi.settings.GlyphPlatform);
        }

        public void SetColor(ColorSetterType colorSetter, Action<Color> setter)
        {
            StartCoroutine(InternalSetColor(colorSetter, setter));
        }

        private IEnumerator InternalSetColor(ColorSetterType colorSetter, Action<Color> setter)
        {
            while(!hasStarted)
            {
                yield return new WaitForEndOfFrame();
            }

            setter(GetColor(colorSetter));
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

        public void ChangeToPc() => ChangeGlyphs(GlyphPlatforms.PC);
        public void ChangeToXbox() => ChangeGlyphs(GlyphPlatforms.XBOX);
        public void ChangeToNintendoSwitch() => ChangeGlyphs(GlyphPlatforms.NINTENDO_SWITCH);
        public void ChangeToPs4() => ChangeGlyphs(GlyphPlatforms.PLAYSTATION_4);
        public void ChangeToPs5() => ChangeGlyphs(GlyphPlatforms.PLAYSTATION_5);
    }
}
