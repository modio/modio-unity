using Modio.API.SchemaDefinitions;
using Modio.Collections;

namespace Modio.Caching
{
    internal class ModCollectionCache : BaseCache<ModCollectionCache, ModCollectionId, ModCollection>
    {
        protected override ModCollection OnGetCached(ModCollectionId key) =>
            Cached.TryGetValue(key, out ModCollection collection)
                ? collection
                : Cached[key] = new ModCollection(key);

        /// <summary>
        /// This is the preferred method if we have a mod collection object; it will populate the Mod with the data if needed
        /// </summary>
        /// <param name="collectionObject">The ModCollectionObject to use</param>
        /// <returns>The ModCollection from the cache, or a new one if it doesn't exist</returns>
        internal static ModCollection Get(ModCollectionObject collectionObject) =>
            Cached.TryGetValue(collectionObject.Id, out ModCollection collection)
                ? collection.ApplyDetailsFromModCollectionObject(collectionObject)
                : Cached[collectionObject.Id] = new ModCollection(collectionObject);
    }
}
