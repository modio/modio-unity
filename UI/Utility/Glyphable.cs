using System;
using System.Collections;
using ModIO.Util;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{

    class Glyphable : MonoBehaviour
    {
        public Image image;
        public GlyphSetting config;

        public void OnValidate() => image = image == null ? GetComponent<Image>() : image;

        void Awake()
        {
            UpdateGlyphs();
            SimpleMessageHub.Instance.Subscribe<MessageGlyphUpdate>(x => UpdateGlyphs());
        }

        public void UpdateGlyphs()
        {
            gameObject.SetActive(true);
            image.sprite = GetGlyphFromDisplayType();
            image.color = Glyphs.Instance.GetColor(config.color);
        }

        private Sprite GetGlyphFromDisplayType()
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
    }
}
