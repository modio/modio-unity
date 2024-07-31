using System.Collections.Generic;

namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct MetadataKvpObject
    {
        public MetadataKvpObject(string key, string value)
        {
            metakey = key;
            metavalue = value;
        }
        public string metakey;
        public string metavalue;
    }
}
