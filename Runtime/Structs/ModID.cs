using System.ComponentModel;
using UnityEngine;

namespace ModIO
{
    /// <summary>
    /// A struct representing the globally unique identifier for a specific mod profile.
    /// </summary>
    [System.Serializable, TypeConverter(typeof(ModIdConverter))]
    public struct ModId
    {
        public static readonly ModId Null = new ModId(0L);

        private long _id;

        public long id { get { return _id; }
            set {
                Debug.Log($"id is changed to {value}");
                _id = value;
            }
        }

        public ModId(long id)
        {
            _id = id;
        }

        public static implicit operator long(ModId id) => id.id;
        public static explicit operator ModId(long id) => new ModId(id);
    }
}
