using System.Collections.Generic;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Newtonsoft.Json;

namespace Modio.Mods
{
    public class ModTag
    {
        static readonly Dictionary<string, ModTag> Tags = new Dictionary<string, ModTag>();

        public readonly string ApiName;
        Dictionary<string, string> _translations;
        public string NameLocalized { get; private set; }
        public bool IsVisible { get; private set; }
        public int Count { get; internal set; }

        ModTag(string apiName) => ApiName = apiName;

        [JsonConstructor]
        public ModTag(string apiName, Dictionary<string, string> translations, string nameLocalized, bool isVisible, int count)
        {
            ApiName = apiName;
            _translations = translations;
            NameLocalized = nameLocalized;
            IsVisible = isVisible;
            Count = count;
        }

        internal static ModTag Get(ModTagObject modTag)
        {
            if (Tags.TryGetValue(modTag.Name, out ModTag tag))
            {
                //in case we previously cached via apiName only
                tag.NameLocalized = modTag.NameLocalized;
                return tag;
            }
            tag = new ModTag(modTag.Name) { NameLocalized = modTag.NameLocalized, };
            Tags.Add(modTag.Name, tag);
            return tag;
        }
        
        internal static ModTag Get(string tagName)
        {
            if (Tags.TryGetValue(tagName, out ModTag tag))
                return tag;
            tag = new ModTag(tagName) { NameLocalized = tagName, };//fallback to API name
            Tags.Add(tagName, tag);
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
