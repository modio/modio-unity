namespace Modio.Mods
{
    public readonly struct ModId
    {
        readonly long _id;

        public ModId(long id) => _id = id;

        public bool IsValid() => _id > 0;

        internal long GetResourceId() => _id;

        public static bool operator ==(ModId left, ModId right) => left._id == right._id;
        public static bool operator !=(ModId left, ModId right) => left._id != right._id;

        public override bool Equals(object obj) => obj is ModId other && this == other;
        public override int GetHashCode() => _id.GetHashCode();

        public static implicit operator long(ModId modId) => modId._id;
        public static implicit operator ModId(long id) => new ModId(id);

        public override string ToString() => _id.ToString();
    }
}
