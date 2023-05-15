using UnityEngine;

namespace ModIOBrowser
{
    public interface ITranslatable
    {
        /// <summary>
        /// Returns the key reference for the item that needs to be translated
        /// </summary>
        string GetReference();

        /// <summary>
        /// Sets the translation onto the ITranslatable object
        /// </summary>
        void SetTranslation(string s);

        /// <summary>
        /// If the object cannot be translated, this method can be applied to mark it for visibility
        /// </summary>
        void MarkAsUntranslated();

        /// <summary>
        /// Sometimes we need to identify the translation to help track it down,
        /// this can be a gameobject name or path, a guid, or whatever is suitable for the task
        /// </summary>
        string Identifier { get; }
        string TransformPath { get; }
    }
}
