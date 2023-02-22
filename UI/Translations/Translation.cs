using System;
using System.Collections.Generic;
using ModIO.Util;
using TMPro;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// Translation is a class which attempts to translate strings on the fly when you change language,
    /// from directly inside another class.
    /// </summary>
    class Translation : ITranslatable
    {        
        public string reference;
        public Action<string> set;
        public string[] valueCache;
        SimpleMessageUnsubscribeToken subscription;

        /// <summary>
        /// Instead of using new, we use Get to make sure that the previous instance of this object is
        /// properly cleaned out.
        /// 
        /// This is because the Translation class is can be tied to complex logic:
        /// a same piece of text may require different translations depending on the state of the
        /// owning class. This is because the text object thats used by this class can have
        /// multiple usages, ie. if a pop up window is using this, it can show different messages.
        ///
        /// In order for the class to work easily with different states, i decided to wrap
        /// instantianting and changing into the same place.
        ///
        /// By using the Get method instead of having to instantiate and set it at different times and places,
        /// it becomes a little bit handier to use.
        /// </summary>
        /// <param name="translation">pointer reference to the text item</param>
        /// <param name="reference">translation reference</param>
        /// <param name="setter">how to set the translation to its corresponding object</param>
        /// <param name="values">any {strings} will be replaced by the values, sequentially</param>
        public static void Get(Translation translation, string reference, Action<string> setter, params string[] values)
        {
            if(translation != null)
            {
                translation.Clear();
            }

            translation = new Translation(setter, reference, values);
        }

        /// <summary>
        /// Instead of using new, we use Get to make sure that the previous instance of this object is
        /// properly cleaned out.
        /// 
        /// This is because the Translation class is can be tied to complex logic:
        /// a same piece of text may require different translations depending on the state of the
        /// owning class. This is because the text object thats used by this class can have
        /// multiple usages, ie. if a pop up window is using this, it can show different messages.
        ///
        /// In order for the class to work easily with different states, i decided to wrap
        /// instantianting and changing into the same place.
        ///
        /// By using the Get method instead of having to instantiate and set it at different times and places,
        /// it becomes a little bit handier to use.
        /// </summary>
        /// <param name="translation">pointer reference to the text item</param>
        /// <param name="reference">translation reference</param>
        /// <param name="text">the TMP_Text object that will be set</param>
        /// <param name="values">any {strings} will be replaced by the values, sequentially</param>
        public static void Get(Translation translation, string reference, TMP_Text text, params string[] values)
            => translation = new Translation(s => text.text = s, reference, values);

        private Translation(Action<string> set, string reference, params string[] values)
        {
            this.set = set;
            this.reference = reference;
            this.valueCache = values;

            this.set(TranslationManager.Instance.Get(reference, valueCache));

            subscription = SimpleMessageHub.Instance.Subscribe<MessageUpdateTranslations>(x =>
            {
                set(TranslationManager.Instance.Get(reference, valueCache));
            });
        }

        public string Identifier => $"TranslationUpdateable for: { reference}";
        public string TransformPath => "N/A memory object";
        public string GetReference() => reference;
        public void MarkAsUntranslated() { set($"<color=\"red\">{reference}</color>"); }
        public void SetTranslation(string s) { set(s); }
        public void Clear() => subscription.Unsubscribe();
    }
}
