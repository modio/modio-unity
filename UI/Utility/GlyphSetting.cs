using UnityEngine;

namespace ModIOBrowser.Implementation
{
    [CreateAssetMenu(fileName = "GlyphSetting.asset", menuName = "ModIo/GlyphSetting")]
    class GlyphSetting : ScriptableObject
    {
        public Glyphs.Glyph glyph;
        public ColorSetterType color;
        public Sprite PC, Xbox, Steamdeck, Playstation4, Playstation5, NintendoSwitch;
    }
}
