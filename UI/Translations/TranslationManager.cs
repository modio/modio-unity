using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using TMPro;
using System.Linq;
using System;
using static ModIO.Utility;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// How to use:
    ///      
    /// Each string you translate needs to exist in the corresponding .po file. It should be located
    /// in resources/mod.io/BrowserLanguages.
    /// 
    /// Any TextMeshProUGUI in the list translated will be automatically translated.
    /// If you are using instanced prefabs, you may not be able to or want to mutate said list,
    /// in that case, use the class "Translatable", or the Get method to change the value when needed.
    ///
    /// You can track down any errors in the translated list by clicking the button "Track down errors",
    /// on the Translator script inside Unity.
    ///
    /// To directly translate a string, use:
    /// TranslationManager.Instance.Get("We were unable to validate your credentials with the mod.io server."));
    /// 
    /// To directly translate a string where values need to be changed or appended, use: 
    /// 
    /// Translator.Instance.Get("I am punching a {ball} using {arms}", "Flower Pot", "Feet"));
    /// 
    /// In this case, it will get translated into "I am punching a Flower Pot using Feet".
    /// (Which is kind of a kick but whatever.)
    /// Get will look for anything inside a {}, and replace it sequentially, so it doesn't really
    /// matter what's inside each {} parameter.
    /// 
    /// In some cases, you aren't able or don't want to use the direct Get translator, for example
    /// when you're working with a prefab in the editor which carries no code, and you don't want to
    /// add a bunch of code just for a simple text change.
    /// 
    /// In that case, you can attach the script "Translatable" to the object. It will automatically
    /// connect itself to the translator and update on runtime. It requires a TextMeshProUGUI variable,
    /// and will add one to the object it is added to, if there is none.
    /// 
    /// Translatable will automatically try to detect if there is any text in the TextMeshProUGUI variable,
    /// and attempt to translate it. It will also automatically an field to English.po, if it doesn't already exist.
    ///
    /// Sometimes you need to change texts on the fly inside the actual code, for that purpose use:
    /// Translation.Get()
    /// </summary>
    class TranslationManager : SimpleMonoSingleton<TranslationManager>
    {
        public TranslatedLanguages SelectedLanguage { get { return Language; } }

        public bool markUntranslatedStringsWithRed = true;
        
        public List<TextMeshProUGUI> translated;

        private TranslatedLanguages Language;
        private List<string> originalTranslationKeyCache = new List<string>();
        private Dictionary<string, string> translations = new Dictionary<string, string>();
        public string attemptToTranslate;
        public List<TextAsset> translationsTextAssets;
        
        protected void Start()
        {
            Language = Browser.Instance.uiConfig.Language;
            Debug.Log($"Language: {Language}");

            CacheTranslatedKeys();
            ForceChangeLanguage(Language);

            Translation t = null;
        }

        /// <summary>
        /// Change language to whatever TranslatedLanguages allows.
        /// This method will automatically translate everything.
        /// </summary>
        public void ChangeLanguage(TranslatedLanguages language)
        {
            if(language == SelectedLanguage)
            {
                Debug.Log($"Already set to {language}!");
                return;
            }

            Debug.Log($"Setting language to {language} from {SelectedLanguage}");
            ForceChangeLanguage(language);
        }

        private void ForceChangeLanguage(TranslatedLanguages language)
        {
            this.Language = language;
            translations = BuildLanguageDictionary(language);            
            ApplyTranslations();
        }

        /// <summary>
        /// Get a translation.
        /// Example 1:
        /// TranslationManager.Instance.Get("Mod installation failed")
        /// 
        /// Example 2:
        /// TranslationManager.Instance.Get("Mod installation failed because {1}", {"my dog ate it."})
        /// returns a translation with the meaning ""Mod installation failed because my dog ate it."
        ///
        /// The Get function will automatically replace anything inside a {} using specified values, sequentially.
        /// 
        /// </summary>
        /// <returns>The translation, or the key of the translation on fail.</returns>
        public string Get(string key, params string[] values) => Get(translations, key, values);

        private void CacheTranslatedKeys() => translated.ForEach(x => originalTranslationKeyCache.Add(x.text));

        public TextAsset GetTranslationResource(TranslatedLanguages language) =>
            translationsTextAssets.FirstOrDefault(x => x.name == language.ToString());

        /// <summary>
        /// Returns a dictionary with the <keys, translations> using the specified language
        /// </summary>
        public static Dictionary<string, string> BuildLanguageDictionary(TranslatedLanguages language)
        {
            TextAsset textAsset = Instance.GetTranslationResource(language);
            Dictionary<string, string> translations = new Dictionary<string, string>();

            if(textAsset != null)
            {
                StringReader reader = new StringReader(textAsset.text);
                string key = null;
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    if(line.StartsWith("msgid \""))
                    {
                        key = line.Substring(7, line.Length - 8);
                    }
                    else if(line.StartsWith("msgstr \"") && key != null)
                    {
                        string val = line.Substring(8, line.Length - 9).Trim();

                        if(translations.ContainsKey(key))
                        {
                            Debug.LogWarning($"Warning: value for {key} - {translations[key]} already exists");
                            translations[key] = val;
                        }
                        else
                            translations.Add(key, val);

                        key = null;
                    }
                }

                reader.Close();
            }
            else
            {
                Debug.Log("Text asset for .po file is null?");
            }
            return translations;
        }

        /// <summary>
        /// Translate everything that is currently hooked in.
        /// </summary>
        private void ApplyTranslations()
        {
            for(int i = 0; i < translated.Count; i++)
            {
                translated[i].text = Get(originalTranslationKeyCache[i]);
            }

            SimpleMessageHub.Instance.Publish(new MessageUpdateTranslations());
        }

        /// <summary>
        /// Translate an ITranslatable. Contains some extra logic to properly identify an object
        /// that fails translation.
        /// </summary>
        /// <param name="translatable"></param>
        public void Translate(ITranslatable translatable)
        {
            if(translations.TryGetValue(translatable.GetReference(), out string translation))
            {
                translatable.SetTranslation(translation);
                return;
            }

            if(markUntranslatedStringsWithRed)
            {
                translatable.MarkAsUntranslated();
            }

            Debug.LogWarning($"The translation for {translatable.GetReference()} on gameobject identifier {translatable.Identifier} is missing.");
        }

        /// <summary>
        /// Immediately get a translated string, using a given translation dictionary.
        /// </summary>
        public static string Get(Dictionary<string, string> translations, string key, params string[] values)
        {
            string translation = key.Trim();

            if(translation != null && translations.TryGetValue(key, out translation))
            {
                if(values == null || values.Length == 0)
                {
                    return translation;
                }

                return ReplaceParameters(translation, values);
            }

            Debug.LogWarning($"Unable to find translation for key: \"{key}\"");
            return "<color=red>" + key + "</color>";
        }

        /// <summary>
        /// Replace anything inside a {} in a string, given a list of values, and return the finished string.
        /// </summary>
        public static string ReplaceParameters(string text, string[] values)
        {
            string originalText = text;
            int i = 0;
            int indexOfClammer = text.IndexOf('{');

            try
            {
                while(indexOfClammer != -1)
                {
                    int indexOfEndClammer = text.IndexOf('}') + 1;
                    string replacedString = text.Substring(indexOfClammer, indexOfEndClammer - indexOfClammer);
                    text = text.Replace(replacedString, values[i]);

                    indexOfClammer = text.IndexOf('{');
                    i++;
                }
            }
            catch(Exception ex)
            {
                //You likely just forgot to add as many {}'s as you have input values!
                Debug.LogError($"translating {originalText} gives error:\n{ex}");
            }
            
            if(i != values.Length)
            {
                Debug.LogWarning($"String of \"{text}\" parameter count did not match expected parameter count, ({values.Length} ");
                return "<color=red>" + text + "</color>";
            }

            return text;
        }


#if UNITY_EDITOR
        /// <summary>
        /// Append a new translation directly into the translation file.
        /// Please note: Due to Unity being finicky, you may need to have this file open in for example Sublime
        /// otherwise Unity will discard changes. Workaround required, but the solution is good enough for now.
        /// </summary>
        public static string AppendTranslation(TranslatedLanguages language, string keyAndContent)
        {
            TextAsset textAsset = Instance.GetTranslationResource(language);
            string addition = $"\nmsgid \"{keyAndContent}\"\nmsgstr \"{keyAndContent}\" \n";

            File.WriteAllText(AssetDatabase.GetAssetPath(textAsset), textAsset.text + addition);
            EditorUtility.SetDirty(textAsset);

            return keyAndContent;
        }

        /// <summary>
        /// For testing purposes, sometimes you need to swap languages on the fly inside the editor.
        /// </summary>
        [ExposeMethodInEditor]
        public void TestChangeLanguageToSwedishRuntimeOnly()
        {
            Language = TranslatedLanguages.Swedish;
            ChangeLanguage(Language);
        }

        /// <summary>
        /// For testing purposes, sometimes you need to swap languages on the fly inside the editor.
        /// </summary>
        [ExposeMethodInEditor]
        public void TestChangeLanguageToEnglishRuntimeOnly()
        {
            Language = TranslatedLanguages.English;
            ChangeLanguage(Language);
        }


        /// <summary>
        /// Track down any errors in the translations list, and compile and show a list of them.
        /// This includes getting a handy transform path to the erroneous object.
        /// </summary>
        [ExposeMethodInEditor]
        public void TrackDownErrors()
        {

            Dictionary<string, string> translations = BuildLanguageDictionary(TranslatedLanguages.English);
            int errorCount = 0;
            var s = "Translation Errors Report\n";
            foreach(TextMeshProUGUI item in translated)
            {
                string translation = Get(translations, item.text);
                if(translation.Contains("<color"))
                {
                    s += $"\n\nExpected error with translation \"{item.text}\"\nPath: {UtilityBrowser.FullPath(item.transform)}";
                    errorCount++;
                }
            }

            Debug.LogError($"{s}\n\nErrors: {errorCount}");
        }

#endif

    }
}
