using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    class Glyphable : MonoBehaviour
    {
        public Image image;
        public GlyphSetting config;

        public void OnValidate() => image = image == null ? GetComponent<Image>() : image;

        private void Awake()
        {
            gameObject.SetActive(false);
            CoroutineRunner.Instance.Run(SetupWhenReady());
        }

        IEnumerator SetupWhenReady()
        {
            while(true)
            {
                if(Glyphs.Instance.ready)
                {
                    gameObject.SetActive(true);
                    image.sprite = GetGlyphFromDisplayType();
                    image.color = Glyphs.Instance.GetColor(config.color);
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private Sprite GetGlyphFromDisplayType()
        {
            switch(Glyphs.Instance.platformType)
            {
                case Glyphs.GlyphPlatforms.PC:
                    return config.PC;
                case Glyphs.GlyphPlatforms.XBOX:
                    return config.Xbox;
                case Glyphs.GlyphPlatforms.PLAYSTATION_4:
                    return config.Playstation4;
                case Glyphs.GlyphPlatforms.PLAYSTATION_5:
                    return config.Playstation5;
                case Glyphs.GlyphPlatforms.NINTENDO_SWITCH:
                    return config.NintendoSwitch;
            }

            Debug.LogWarning($"{gameObject.name} is missing configuration for {Glyphs.Instance.platformType}");
            return Glyphs.Instance.fallbackSprite;
        }
    }
}
