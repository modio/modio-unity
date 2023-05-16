using System;
using System.Collections;
using ModIO.Util;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{

    internal class Glyphable : MonoBehaviour
    {
        public Image image;
        public GlyphSetting config;
        SimpleMessageUnsubscribeToken subToken;

        public void OnValidate() => image = image == null ? GetComponent<Image>() : image;

        void Start()
        {
            UpdateGlyphs();
            subToken = SimpleMessageHub.Instance.Subscribe<MessageGlyphUpdate>(x => UpdateGlyphs());
        }

        public void UpdateGlyphs()
        {
            var glyph = GetGlyphFromDisplayType();

            if(glyph != null)
            {
                gameObject.SetActive(true);
                image.sprite = glyph;
                Glyphs.Instance.SetColor(config.color, x => image.color = x);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        Sprite GetGlyphFromDisplayType()
        {
            switch(Glyphs.Instance.PlatformType)
            {
                case GlyphPlatforms.PC: return config.PC;
                case GlyphPlatforms.XBOX: return config.Xbox;
                case GlyphPlatforms.PLAYSTATION_4: return config.Playstation4;
                case GlyphPlatforms.PLAYSTATION_5: return config.Playstation5;
                case GlyphPlatforms.NINTENDO_SWITCH: return config.NintendoSwitch;
            }

            Debug.LogWarning($"{gameObject.name} is missing configuration for {Glyphs.Instance.PlatformType}");
            return Glyphs.Instance.fallbackSprite;
        }

        void OnDestroy()
        {
            subToken?.Unsubscribe();
        }
    }
}
