using System.Collections.Generic;

namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct GameTagOptionObject
    {
        public string name;
        public string type;
        public string[] tags;
        public Dictionary<string, int> tag_count_map;
        public bool hidden;
        public bool locked;
    }
}
