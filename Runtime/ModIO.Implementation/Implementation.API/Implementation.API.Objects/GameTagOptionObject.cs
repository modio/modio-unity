using System.Collections.Generic;

namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct GameTagOptionObject
    {
        public string name;
        public Dictionary<string, string> name_localization;
        public string type;
        public string[] tags;
        public GameTagOptionLocalizationObject[] tags_localization;
        public Dictionary<string, int> tag_count_map;
        public bool hidden;
        public bool locked;
    }
}
