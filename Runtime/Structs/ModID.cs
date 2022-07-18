using System.ComponentModel;

namespace ModIO
{
    /// <summary>
    /// A struct representing the globally unique identifier for a specific mod profile.
    /// </summary>
    [System.Serializable, TypeConverter(typeof(ModIdConverter))]
    public struct ModId
    {
        public static readonly ModId Null = new ModId(0L);

        public long id;

        public ModId(long id)
        {
            this.id = id;
        }

        public static implicit operator long(ModId id) => id.id;
        public static explicit operator ModId(long id) => new ModId(id);
    }
}
