namespace Modio.Collections
{
    public struct ModCollectionId
    {
        readonly long _id;

        public ModCollectionId(long id) => _id = id;

        public bool IsValid() => _id > 0;

        internal long GetResourceId() => _id;

        public static bool operator ==(ModCollectionId left, ModCollectionId right) => left._id == right._id;

        public static bool operator !=(ModCollectionId left, ModCollectionId right) => left._id != right._id;

        public override bool Equals(object obj) => obj is ModCollectionId other && this == other;

        public override int GetHashCode() => _id.GetHashCode();

        public static implicit operator long(ModCollectionId collectionId) => collectionId._id;

        public static implicit operator ModCollectionId(long id) => new ModCollectionId(id);

        public override string ToString() => _id.ToString();
    }
}
