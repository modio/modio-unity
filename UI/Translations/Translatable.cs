using UnityEngine;
using TMPro;
using System.Collections.Generic;
using static ModIO.Utility;

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

        //TODO: Replace with TMP_Text?
        public TextMeshProUGUI text; 

        public string Identifier => gameObject.name;

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

        /// <summary>
        /// Immediately translates item
        /// Also hooks Translatable up so it listens to the MessageUpdateTranslations message,
        /// which automatically triggers a retranslation of the object.
        /// </summary>
        private void Awake()
        {
            SimpleMessageHub.Instance.Subscribe<MessageUpdateTranslations>(
                x => TranslationManager.Instance.Translate(this));
        }

        public void Start() => TranslationManager.Instance.Translate(this);
    }
}
