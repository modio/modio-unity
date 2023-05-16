using System;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using ModIO.Util;
using Plugins.mod.io.UI.Translations;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// Most of the time we can directly install a TextMeshProUGUI on the Translator master object.
    /// However, sometimes this isn't useful - for example when it's on an prefab that's instantiated
    /// during runtime.
    /// Thats where this class comes in. Attach it to the prefab and drag its TextMeshProUGui text
    /// to the translatable and it'll take care of the rest.
    ///
    /// If the TextMeshProUGUI contains a text which is not translated, the Translatable class will
    /// attempt to add it to the dictionary. However, sometimes Unity can muddy that file, and that operation
    /// fails. However, if you keep the file open in Sublime or similar text editing apps, tabbing onto
    /// it is usually enough to make sure Unity doesnt restore the file.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    class Translatable : MonoBehaviour, ITranslatable
    {
        private const TranslatedLanguages EditorLanguage = TranslatedLanguages.English;
        private const bool AddTextIfItDoesntExist = true;

        public string reference;

        public TextMeshProUGUI text;

        public TranslatedLanguageFontPairings translatedLanguageFontPairingOverrides;

        public string Identifier => gameObject.name;

        public string TransformPath => transform.FullPath();

#if UNITY_EDITOR
        /// <summary>
        /// If the TextMeshProUGUI contains a text which is not translated, the Translatable class will
        /// attempt to add it to the dictionary. However, sometimes Unity can muddy that file, and that operation
        /// fails. However, if you keep the file open in Sublime or similar text editing apps, tabbing onto
        /// it is usually enough to make sure Unity doesnt restore the file.
        /// </summary>
        //TODO: Can probably fix unitys shaky behaviour regarding text file manipulation, see above comments.
        private void OnValidate()
        {
            if(text == null)
            {
                text = GetComponent<TextMeshProUGUI>();
                if(text == null)
                {
                    Debug.Log("Unable to find text field, unable to apply translation.");
                    return;
                }

                Dictionary<string, string> lang = TranslationManager.BuildLanguageDictionary(EditorLanguage);
                string potentialKey = text.text;
                if(lang.ContainsKey(potentialKey))
                {
                    reference = potentialKey;
                }
                else
                {
                    if(AddTextIfItDoesntExist)
                    {
                        reference = TranslationManager.AppendTranslation(EditorLanguage, text.text);
                    }
                }
            }
        }
#endif

        public string GetReference() => reference;
        public void SetTranslation(string s) => text.text = s;

        /// <summary>
        /// Mark the text field clearly with red so that we know it needs to be added to the translator
        /// </summary>
        public void MarkAsUntranslated() => text.text = $"<color=\"red\">{text.text}</color>";

        private SimpleMessageUnsubscribeToken subToken;

        /// <summary>
        /// Immediately translates item
        /// Also hooks Translatable up so it listens to the MessageUpdateTranslations message,
        /// which automatically triggers a retranslation of the object.
        /// </summary>
        private void Awake()
        {
            subToken = SimpleMessageHub.Instance.Subscribe<MessageUpdateTranslations>(
                (s)=>ApplyTranslation());
        }

        public void Start() => ApplyTranslation();

        private void ApplyTranslation()
        {
            if(TranslationManager.SingletonIsInstantiated())
            {
                TranslationManager.Instance.Translate(this);
                if(translatedLanguageFontPairingOverrides == null)
                    return;

                var selectedLanguage = TranslationManager.Instance.SelectedLanguage;
                //Look for an override font for the selected language
                if(translatedLanguageFontPairingOverrides.GetFontAsset(selectedLanguage)
                   is TMP_FontAsset fontAssetOverride)
                {
                    this.text.font = fontAssetOverride;
                }
                //Look for the default font for the selected language
                else if(TranslationManager.Instance.defaultTranslatedLanguageFontPairings.GetFontAsset(selectedLanguage)
                        is TMP_FontAsset fontAsset)
                {
                    this.text.font = fontAsset;
                }
            }            
        }

        private void OnDestroy()
        {
            subToken.Unsubscribe();
        }
    }
}
