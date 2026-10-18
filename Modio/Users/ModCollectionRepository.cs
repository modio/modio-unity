using System;
using System.Collections.Generic;
using System.Linq;
using Modio.API.SchemaDefinitions;
using Modio.Collections;
using Modio.Mods;
using Modio.Ratings;

namespace Modio.Users
{
    public class ModCollectionRepository : IDisposable
    {

        internal event Action OnContentsChanged;
        
        readonly HashSet<ModCollection> _followed = new HashSet<ModCollection>();
        readonly Dictionary<ModioId, (ModioRating rating, long timestamp)> _ratings = new Dictionary<ModioId, (ModioRating rating, long timestamp)>();
        
        public IEnumerable<ModCollection> GetFollowed() => _followed;

        internal ModCollectionRepository()
        {
            ModCollection.AddChangeListener(ModCollectionChangeType.IsFollowed, OnModCollectionFollowChange);
            ModCollection.AddChangeListener(ModCollectionChangeType.Rating, OnModCollectionRatingChange);
            ModioClient.OnShutdown += Dispose;
        }

        void OnModCollectionFollowChange(ModCollection mod, ModCollectionChangeType changeType)
        {
            var anyChange = false;
            if (mod.IsFollowed)
                anyChange |= _followed.Add(mod);
            else
                anyChange |= _followed.Remove(mod);

            if(anyChange) OnContentsChanged?.Invoke();
        }

        public bool IsFollowed(ModioId modId) => _followed.Any(mod => mod.Id == modId);
        
        public bool TryGetRating(ModioId id, out ModioRating rating)
        {
            rating = default(ModioRating);
            if (!_ratings.TryGetValue(id, out (ModioRating rating, long timestamp) pair))
                return false;

            rating = pair.rating;
            return true;
        }

        internal void SetUserRatingFromObject(RatingObject rating)
        {
            _ratings[rating.ModId] = ((ModioRating)rating.Rating, rating.DateAdded);
        }

        void OnModCollectionRatingChange(ModCollection modCollection, ModCollectionChangeType changeType)
        {
            if (modCollection.CurrentUserRating == ModioRating.None)
            {
                _ratings.Remove(modCollection.Id);
                return;
            }

            if (!_ratings.TryGetValue(modCollection.Id, out (ModioRating rating, long timestamp) ratingPair))
            {
                ratingPair = (modCollection.CurrentUserRating, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                _ratings[modCollection.Id] = ratingPair;
            }
            else if(ratingPair.rating != modCollection.CurrentUserRating)
            {
                ratingPair.rating = modCollection.CurrentUserRating;
                _ratings[modCollection.Id] = ratingPair;
            }
            
        }
        public IEnumerable<(ModioId id, long timestamp)> GetAllRatedModCollectionIds() => _ratings.Select(pair => (pair.Key, pair.Value.timestamp));

        public void Dispose()
        {
            ModCollection.RemoveChangeListener(ModCollectionChangeType.IsFollowed, OnModCollectionFollowChange);
            ModCollection.RemoveChangeListener(ModCollectionChangeType.Rating, OnModCollectionRatingChange);
            ModioClient.OnShutdown -= Dispose;
        }
    }
}
