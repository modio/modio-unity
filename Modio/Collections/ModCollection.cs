using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Caching;
using Modio.Errors;
using Modio.Extensions;
using Modio.Images;
using Modio.Mods;
using Modio.Users;
using Plugins.Modio.Modio.Ratings;

namespace Modio.Collections
{
    public class ModCollection
    {
        /// <summary>Posts an event whenever this mod collection has been updated.</summary>
        public event Action OnModCollectionUpdated;

        /// <summary>Adds an event handler to listen for whenever the <see cref="ModCollectionChangeType"/> of a collection
        /// is changed.</summary>
        /// <remarks><see cref="ModCollectionChangeType"/> is a bit flag, multiple changes can be listened for with one
        /// handler.</remarks>
        public static void AddChangeListener(
            ModCollectionChangeType subscribedChange,
            Action<ModCollection, ModCollectionChangeType> listener
        )
        {
            if (ChangeSubscribers.TryGetValue(
                    subscribedChange,
                    out Action<ModCollection, ModCollectionChangeType> existing
                ))
                listener = existing + listener;

            ChangeSubscribers[subscribedChange] = listener;
        }

        /// <summary>
        /// Removes an event handler that listens for changes to the <see cref="ModCollectionChangeType"/> of a collection.
        /// </summary>
        /// <param name="subscribedChange">The type of change to unsubscribe from.</param>
        /// <param name="listener">The event handler to remove.</param>
        /// <remarks>This will only remove the handler if it was previously added with <see cref="AddChangeListener"/>.</remarks>
        public static void RemoveChangeListener(
            ModCollectionChangeType subscribedChange,
            Action<ModCollection, ModCollectionChangeType> listener
        )
        {
            if (ChangeSubscribers.TryGetValue(subscribedChange, out Action<ModCollection, ModCollectionChangeType> _))
                ChangeSubscribers[subscribedChange] -= listener;
        }

        /// <summary>
        /// Holds a dictionary of <see cref="ModCollectionChangeType"/> and their associated event handlers.
        /// </summary>
        static readonly Dictionary<ModCollectionChangeType, Action<ModCollection, ModCollectionChangeType>>
            ChangeSubscribers =
                new Dictionary<ModCollectionChangeType, Action<ModCollection, ModCollectionChangeType>>();

#region ModCollectionProperties

        /// <summary>The collection id.</summary>
        internal ModCollectionId Id { get; }
        /// <summary>The game id.</summary>
        internal long GameId { get; private set; }
        /// <summary>The status of the collection.</summary>
        internal long Status { get; private set; }
        /// <summary>Visibility status of the collection.</summary>
        internal bool Visible { get; set; }
        /// <summary>The user who submitted the collection.</summary>
        internal UserProfile Creator { get; private set; }
        /// <summary>The category of the collection.</summary>
        internal string Category { get; set; }
        /// <summary>The date the collection was added.</summary>
        internal DateTime DateAdded { get; set; }
        /// <summary>The date the collection was last updated.</summary>
        internal DateTime DateUpdated { get; private set; }
        /// <summary>The date the collection went live.</summary>
        internal DateTime DateLive { get; private set; }
        /// <summary>The maximum limit of mods allowed in this collection.</summary>
        internal long LimitNumberMods { get; set; }
        /// <summary>The maturity options detected within this collection.</summary>
        internal ModMaturityOptions MaturityOptions { get; private set; }
        /// <summary>The total filesize of all mods in the collection.</summary>
        internal long ArchiveFilesize { get; set; }
        /// <summary>The total uncompressed filesize of all mods in the collection.</summary>
        internal long Filesize { get; set; }
        /// <summary>The platforms the mods are compatible within this collection.</summary>
        internal string[] Platforms { get; set; }
        /// <summary>The tags associated with the collection.</summary>
        internal ModTag[] Tags { get; private set; }
        /// <summary>The stats of the collection.</summary>
        internal ModCollectionStats Stats { get; private set; }
        /// <summary>The logo of the collection.</summary>
        public ModioImageSource<Mod.LogoResolution> Logo { get; private set; }
        /// <summary>The name of the collection.</summary>
        internal string Name { get; private set; }
        /// <summary>The name id of the collection.</summary>
        internal string NameId { get; private set; }
        /// <summary>The summary of the collection.</summary>
        internal string Summary => _summaryDecoded ??= WebUtility.HtmlDecode(_summaryHtmlEncoded);
        /// <summary>The description of the collection.</summary>
        internal string Description { get; private set; }

        /// <summary>Whether the collection is followed by the user.</summary>
        public bool IsFollowed { get; private set; }

        Mod[] _mods;

        public static async Task<(Error error, ModioPage<ModCollection> page)> GetCollections(
            ModioAPI.Collections.GetModCollectionsFilter filter
        )
        {
            string searchCacheKey = ModCollectionCache.ConstructFilterKey(filter);

            if (ModCollectionCache.GetCachedSearch(
                    filter,
                    searchCacheKey,
                    out ModCollection[] cachedCollection,
                    out long resultTotal
                ))
            {
                var pagination = new ModioPage<ModCollection>(
                    cachedCollection,
                    filter.PageSize,
                    filter.PageIndex,
                    resultTotal
                );

                return (Error.None, pagination);
            }

            (Error error, Pagination<ModCollectionObject[]>? modObjects) =
                await ModioAPI.Collections.GetModCollections(filter);

            if (error)
                return (error, null);

            int resultCount = modObjects.Value.Data.Length;
            ModCollection[] mods = resultCount == 0 ? Array.Empty<ModCollection>() : new ModCollection[resultCount];

            for (var i = 0; i < mods.Length; i++)
                mods[i] = ModCollectionCache.Get(modObjects.Value.Data[i]);

            long pageSize = modObjects.Value.ResultLimit;
            long pageIndex = modObjects.Value.ResultOffset / pageSize;

            ModCollectionCache.CacheSearch(searchCacheKey, mods, pageIndex, modObjects.Value.ResultTotal);

            var page = new ModioPage<ModCollection>(
                mods,
                (int)pageSize,
                pageIndex,
                modObjects.Value.ResultTotal
            );

            return (Error.None, page);
        }

#endregion

        string _summaryHtmlEncoded;
        string _summaryDecoded;

        internal ModCollection(ModCollectionId id) => Id = id;

        internal ModCollection(ModCollectionObject modCollectionObject)
        {
            Id = modCollectionObject.Id;
            ApplyDetailsFromModCollectionObject(modCollectionObject);
        }

        public static ModCollection Get(long id) => ModCollectionCache.GetCached(new ModCollectionId(id));

        internal ModCollection ApplyDetailsFromModCollectionObject(ModCollectionObject modObject)
        {
            Name = WebUtility.HtmlDecode(modObject.Name);
            NameId = modObject.NameId;

            GameId = modObject.GameId;
            Status = modObject.Status;
            Visible = modObject.Visible;
            Category = modObject.Category;
            LimitNumberMods = modObject.LimitNumberMods;
            ArchiveFilesize = modObject.Filesize;
            Filesize = modObject.FilesizeUncompressed;
            Platforms = modObject.Platforms;

            _summaryHtmlEncoded = modObject.Summary;
            _summaryDecoded = null;
            Description = modObject.Description;

            Creator = UserProfile.Get(modObject.SubmittedBy);

            DateAdded = modObject.DateAdded.GetLocalDateTime();
            DateLive = modObject.DateLive.GetLocalDateTime();
            DateUpdated = modObject.DateUpdated.GetLocalDateTime();

            Tags = modObject.Tags.Select(ModTag.Get).ToArray();

            MaturityOptions = (ModMaturityOptions)modObject.MaturityOption;

            Logo = new ModioImageSource<Mod.LogoResolution>(
                modObject.Logo.Filename,
                modObject.Logo.Thumb320X180,
                modObject.Logo.Thumb640X360,
                modObject.Logo.Thumb1280X720,
                modObject.Logo.Original
            );

            Stats = new ModCollectionStats(modObject.Stats);

            InvokeModCollectionUpdated(ModCollectionChangeType.Everything);
            return this;
        }

#region Mods

        /// <summary>Gets all mods that qualify the provided <see cref="ModioAPI.Collections.GetCollectionMods" /> parameters.</summary>
        /// <remarks>
        /// This will cache searches and results. If a search exists in the cache, this method will
        /// return those results.<br />
        /// The <see cref="ModioAPI.Collections.GetCollectionModsFilter" /> is used to filter the results, allowing
        /// for pagination, sorting, and other search parameters.
        /// </remarks>
        public static async Task<(Error error, ModioPage<Mod> page)> GetCollectionMods(
            long collectionId,
            ModioAPI.Collections.GetCollectionModsFilter filter
        )
        {
            string searchCacheKey = ModCache.ConstructFilterKey(filter);

            if (ModCache.GetCachedModSearch(filter, searchCacheKey, out Mod[] cachedMods, out long resultTotal))
            {
                var pagination = new ModioPage<Mod>(
                    cachedMods,
                    filter.PageSize,
                    filter.PageIndex,
                    resultTotal
                );

                return (Error.None, pagination);
            }

            (Error error, Pagination<ModObject[]>? modObjects) =
                await ModioAPI.Collections.GetCollectionMods(collectionId, filter);

            if (error)
                return (error, null);

            int resultCount = modObjects.Value.Data.Length;
            Mod[] mods = resultCount == 0 ? Array.Empty<Mod>() : new Mod[resultCount];

            for (var i = 0; i < mods.Length; i++)
                mods[i] = ModCache.GetMod(modObjects.Value.Data[i]);

            long pageSize = modObjects.Value.ResultLimit;
            long pageIndex = modObjects.Value.ResultOffset / pageSize;

            ModCache.CacheModSearch(searchCacheKey, mods, pageIndex, modObjects.Value.ResultTotal);

            var page = new ModioPage<Mod>(
                mods,
                (int)pageSize,
                pageIndex,
                modObjects.Value.ResultTotal
            );

            return (Error.None, page);
        }
        
        /// <summary>
        /// Gets all mods of the collection.
        /// </summary>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="IReadOnlyList{Mod}"/> results), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>result</c> is a readonly list of <see cref="Mod"/> in the collection.</p>
        /// </returns>
        public async Task<(Error error, IReadOnlyList<Mod> results)> GetMods()
        {
            Error error;

            if (_mods != null)
                return (Error.None, _mods);

            ModioAPI.Collections.GetCollectionModsFilter filter = ModioAPI.Collections.FilterGetCollectionMods();

            List<ModObject> modObjects;

            (error, modObjects) = await Pagination<ModObject>.CrawlAllPages(
                filter,
                (long)Id,
                ModioAPI.Collections.GetCollectionMods
            );

            if (error)
                return (error, Array.Empty<Mod>());

            _mods = new Mod[modObjects.Count];

            for (var i = 0; i < modObjects.Count; i++)
            {
                ModObject modObject = modObjects[i];
                _mods[i] = ModCache.GetMod(modObject);
            }

            InvokeModCollectionUpdated(ModCollectionChangeType.ModList);

            return (Error.None, _mods);
        }

#endregion

#region Subscriptions

        /// <summary>
        /// Subscribe to all mods in this collection.
        /// </summary>
        /// <returns>An <see cref="Error"/> indicating the success or failure of the operation.</returns>
        public Task<Error> Subscribe() => SetSubscribeToAllMods(true);

        /// <summary>
        /// Unsubscribe from all mods in this collection.
        /// </summary>
        /// <returns>An <see cref="Error"/> indicating the success or failure of the operation.</returns>
        public Task<Error> Unsubscribe() => SetSubscribeToAllMods(false);

        /// <summary>
        /// Helps to subscribe or unsubscribe from all mods in this collection.
        /// </summary>
        /// <param name="subscribed"></param>
        /// <returns>An <see cref="Error"/> indicating the success or failure of the operation.</returns>
        async Task<Error> SetSubscribeToAllMods(bool subscribed)
        {
            Error error;
            ModCollectionObject? collectionObject;

            if (subscribed)
                (error, collectionObject) = await ModioAPI.Collections.SubscribeToCollectionMods(Id.ToString());
            else
                (error, collectionObject) = await ModioAPI.Collections.UnsubscribeFromCollectionMods(Id.ToString());

            if (error)
                return error;

            if (collectionObject.HasValue)
                ApplyDetailsFromModCollectionObject(collectionObject.Value);

            await User.Current.SyncSubscriptions();

            return error;
        }

#endregion

#region Following

        /// <summary>
        /// Follow this collection.
        /// </summary>
        /// <returns>An <see cref="Error"/> indicating the success or failure of the operation.</returns>
        public Task<Error> Follow() => SetFollowed(true);

        /// <summary>
        /// Unfollow this collection.
        /// </summary>
        /// <returns>An <see cref="Error"/> indicating the success or failure of the operation.</returns>
        public Task<Error> Unfollow() => SetFollowed(false);

        /// <summary>
        /// Helps to follow or unfollow this collection.
        /// </summary>
        /// <param name="followIntent">The intent to follow or unfollow the collection.</param>
        /// <returns>An <see cref="Error"/> indicating the success or failure of the operation.</returns>
        async Task<Error> SetFollowed(bool followIntent)
        {
            if (IsFollowed == followIntent)
                return Error.None;

            //Set pending follow
            UpdateLocalFollowStatus(followIntent);

            Error error;

            if (followIntent)
            {
                ModCollectionObject? collection;
                (error, collection) = await ModioAPI.Collections.FollowCollection(Id);

                if (collection.HasValue)
                    ApplyDetailsFromModCollectionObject(collection.Value);
            }
            else
                (error, _) = await ModioAPI.Collections.UnfollowCollection(Id);

            switch (followIntent)
            {
                case true
                    when error:
                    UpdateLocalFollowStatus(false);
                    break;

                case false when (error && error.Code != ErrorCode.CANNOT_OPEN_CONNECTION):
                    UpdateLocalFollowStatus(true);
                    break;
            }

            InvokeModCollectionUpdated(ModCollectionChangeType.IsFollowed);
            return error;
        }

        /// <summary>
        /// Updates the local follow status of this collection.
        /// </summary>
        /// <param name="followed"></param>
        internal void UpdateLocalFollowStatus(bool followed)
        {
            if (IsFollowed == followed)
                return;

            IsFollowed = followed;
        }

#endregion

#region Rating

        /// <summary>
        /// Rate this collection.
        /// </summary>
        /// <param name="rating">The rating to give the collection.</param>
        /// <returns>An <see cref="Error"/> indicating the success or failure of the operation.</returns>
        public async Task<Error> Rate(ModioRating rating)
        {
            InvokeModCollectionUpdated(ModCollectionChangeType.Rating);
            var body = new AddRatingRequest((long)rating);
            (Error error, _) = await ModioAPI.Ratings.AddCollectionRating(Id, body);

            if (error)
            {
                if (!error.IsSilent)
                    ModioLog.Warning?.Log($"Error rating mod {Id}: {error.GetMessage()}");

                InvokeModCollectionUpdated(ModCollectionChangeType.Rating);
            }

            return error;
        }

#endregion

        /// <summary>
        /// Invokes the <see cref="OnModCollectionUpdated"/> event and notifies all subscribers
        /// </summary>
        /// <param name="changeFlags">The flags indicating what has changed in the collection.</param>
        void InvokeModCollectionUpdated(ModCollectionChangeType changeFlags)
        {
            foreach ((ModCollectionChangeType changeType,
                      Action<ModCollection, ModCollectionChangeType> listeners) in ChangeSubscribers)
                if ((changeType & changeFlags) != 0)
                    listeners?.Invoke(this, changeFlags);

            OnModCollectionUpdated?.Invoke();
        }
    }
}
