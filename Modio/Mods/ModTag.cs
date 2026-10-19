using System.Collections.Generic;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Newtonsoft.Json;

namespace Modio.Mods
{
    public enum ResourceTagType
    {
        ModTag,
        CollectionCategory,
        CollectionTag,
    }
    
    public class ModTag
    {
        static readonly Dictionary<(string, ResourceTagType), ModTag> Tags = new Dictionary<(string, ResourceTagType), ModTag>();

        public readonly string ApiName;
        [JsonProperty]
        Dictionary<string, string> _translations;

        /// <summary>
        /// A cached localized name of the tag, based on the language code sent to the API
        /// </summary>
        public string NameLocalized { get; private set; }
        
        [JsonProperty]
        bool _isVisible;
        [JsonIgnore]
        public bool IsVisible
        {
            get => _isVisible && !TempHidden;
            internal set => _isVisible = value;
        }
        
        /// <summary>
        /// Should this tag be hidden for the user temporarily
        /// We use this when filtering the entire mods page for a particular search
        ///
        /// e.g. you want to have separate "maps" and "characters" pages using a tag filter,
        /// but don't want the tags to be visible on those pages
        /// </summary>
        [JsonIgnore]
        public bool TempHidden { get; set; }
        
        public ResourceTagType TagType { get; }
        public int Count { get; internal set; }

        ModTag(string apiName, ResourceTagType tagType)
        {
            ApiName = apiName;
            TagType = tagType;
        }

        [JsonConstructor]
        public ModTag(string apiName, Dictionary<string, string> _translations, string nameLocalized, bool _isVisible, int count)
        {
            ApiName = apiName;
            this._translations = _translations;
            NameLocalized = _translations?.GetValueOrDefault(ModioAPI.LanguageCodeResponse) ?? nameLocalized;
            this._isVisible = _isVisible;
            Count = count;
        }

        internal static ModTag Get(ModTagObject modTag) => Get(modTag, ResourceTagType.ModTag);
        internal static ModTag Get(ModTagObject modTag, ResourceTagType tagType)
        {
            if (Tags.TryGetValue((modTag.Name, tagType), out ModTag tag))
            {
                //in case we previously cached via apiName only
                tag.NameLocalized = modTag.NameLocalized;
                return tag;
            }

            tag = new ModTag(modTag.Name, tagType) { NameLocalized = modTag.NameLocalized, };
            Tags.Add((modTag.Name, tagType), tag);
            return tag;
        }
        
        public static ModTag Get(string tagName, ResourceTagType tagType = ResourceTagType.ModTag)
        {
            if (Tags.TryGetValue((tagName, tagType), out ModTag tag))
                return tag;

            tag = new ModTag(tagName, tagType) { NameLocalized = tagName, };//fallback to API name
            Tags.Add((tagName, tagType), tag);
            return tag;
        }

        public void SetLocalizations(Dictionary<string,string> translations)
        {
            _translations = translations;

            if (translations.TryGetValue(ModioAPI.LanguageCodeResponse, out string translation))
                NameLocalized = translation;
        }
    }
}
