using System;
using System.Collections.Generic;
using ModIOBrowser.Implementation;
using TMPro;
using UnityEngine;

namespace Plugins.mod.io.UI.Translations
{
    [CreateAssetMenu(fileName = "LanguageFontPairings.asset", menuName = "ModIo/LanguageFontPairings")]
    public class TranslatedLanguageFontPairings : ScriptableObject
    {
        [Serializable]
        public class FontPairing
        {
            public TranslatedLanguages TranslatedLanguage;
            public TMP_FontAsset FontAsset;
        }

        public List<FontPairing> translatedLanguageFontPairing = new List<FontPairing>();

        public TMP_FontAsset GetFontAsset(TranslatedLanguages translatedLanguage)
        {
            foreach(var fontPairing in this.translatedLanguageFontPairing)
            {
                if(fontPairing.TranslatedLanguage == translatedLanguage)
                {
                    return fontPairing.FontAsset;
                }
            }

            return null;
        }


    }
}
