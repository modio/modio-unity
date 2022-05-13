
using System;

namespace ModIOBrowser
{
    [Serializable]
    public struct ColorSchemeBlock
    {
        public static ColorSchemeBlock DefaultColorSchemeBlock = new ColorSchemeBlock()
        {
            Normal = ColorSetterType.LightGrey2,
            Highlighted = ColorSetterType.Accent,
            Pressed = ColorSetterType.Accent,
            Disabled = ColorSetterType.Dark3,
            NormalColorAlpha = 1f,
            HighlightedColorAlpha = 1f,
            PressedColorAlpha = 1f,
            DisabledColorAlpha = 1f,
            ColorMultiplier = 1f,
            FadeDuration = 0.1f
        };
        
        public ColorSetterType Normal;
        public float NormalColorAlpha;
        public ColorSetterType Highlighted;
        public float HighlightedColorAlpha;
        public ColorSetterType Pressed;
        public float PressedColorAlpha;
        public ColorSetterType Disabled;
        public float DisabledColorAlpha;
        public float ColorMultiplier;
        public float FadeDuration;
    }
}
