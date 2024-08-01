using System.Collections.Generic;

namespace ModIO
{
    /// <summary>
    /// Represents a particular category of tags.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetTagCategories"/>
    /// <seealso cref="ModIOUnityAsync.GetTagCategories"/>
    /// <seealso cref="Tag"/>
    [System.Serializable]
    public struct TagCategory
    {
        public string name;
        /// <summary>Localized category name, keyed by language code.</summary>
        public Dictionary<string, string> nameLocalized;
        public Tag[] tags;
        /// <summary>Localized tags, keyed by language code.<br />Order matches <see cref="tags"/>, however English value is also available using <c>tagsLocalized["en"]</c>.</summary>
        /// <seealso cref="ModIOUnity.GetTagLocalized(string)"/>
        /// <seealso cref="ModIOUnity.GetTagLocalized(string, string)"/>
        public Dictionary<string, string>[] tagsLocalized;
        public bool multiSelect;
        public bool hidden;
        public bool locked;
    }
}
