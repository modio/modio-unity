using System.ComponentModel;

namespace ModIO
{
    /// <summary>
    /// A struct representing the globally unique identifier for a specific mod profile.
    /// </summary>
    [System.Serializable, TypeConverter(typeof(ModIdConverter))]
    public readonly struct ModId
    {
        public static readonly ModId Null = new ModId(0L);

        public readonly long id;

        public ModId(long id)
        {
            this.id = id;
        }

        public static implicit operator long(ModId id) => id.id;
        public static explicit operator ModId(long id) => new ModId(id);

        public static bool operator ==(ModId left, ModId right) => left.id == right.id;
        public static bool operator !=(ModId left, ModId right) => left.id != right.id;

        public static bool operator ==(ModId left, long right) => left.id == right;
        public static bool operator !=(ModId left, long right) => left.id != right;
        public static bool operator ==(long left, ModId right) => right == left;
        public static bool operator !=(long left, ModId right) => right != left;

        public bool Equals(ModId other) => this == other;
        public override bool Equals(object obj) => obj is ModId other && this == other;
        public override int GetHashCode() => id.GetHashCode();
    }
}
