using System;
using System.Linq;
using System.Threading.Tasks;
using Modio.API.SchemaDefinitions;
using Newtonsoft.Json;

namespace Modio.Mods
{
    public class GameTagCategory
    {
        static GameTagCategory[] _cachedTags;
        
        public readonly string Name;
        public readonly bool MultiSelect;
        public readonly ModTag[] Tags; 
        public readonly bool Hidden;
        public readonly bool Locked;

        [JsonConstructor]
        internal GameTagCategory(string name, bool multiSelect, ModTag[] tags, bool hidden, bool locked)
        {
            Name = name;
            MultiSelect = multiSelect;
            Tags = tags;
            Hidden = hidden;
            Locked = locked;
        }
        
        internal GameTagCategory(GameTagOptionObject tagObject){
            Name = tagObject.Name;
            MultiSelect = tagObject.Type == "checkboxes";
            Hidden = tagObject.Hidden;
            Locked = tagObject.Locked;
            Tags = tagObject.Tags.Select(ModTag.Get).ToArray();
            
            foreach ((string tagName, int count) in tagObject.TagCountMap)
            {
                ModTag tag = ModTag.Get(tagName);
                tag.Count = count;
            }

            if (tagObject.TagsLocalization != null)
                foreach (var localization in tagObject.TagsLocalization)
                {
                    ModTag tag = ModTag.Get(localization.Tag);
                    tag.SetLocalizations(localization.Translations);
                }
        }

        static GameTagCategory()
        {
            //handle restarting the plugin and potentially changing game
            ModioClient.OnInitialized += () => _cachedTags = null;
        }

        public static async Task<(Error, GameTagCategory[])> GetGameTagOptions()
        {
            if (_cachedTags != null) return (Error.None, _cachedTags);
            
            (Error error, Pagination<GameTagOptionObject[]>? gameTagOptionObjects) = await API.ModioAPI.Tags.GetGameTagOptions();
            
            if (error)
            {
                (Error readCacheError, GameData cachedGameData) = await ModioClient.DataStorage.ReadGameData();

                //Note we return the web error, not the cache error, as that's more useful
                if (readCacheError) return (error, Array.Empty<GameTagCategory>());
                
                _cachedTags = cachedGameData.Categories;
                return (Error.None, _cachedTags);
            }

            _cachedTags = gameTagOptionObjects.Value.Data.Select(options => new GameTagCategory(options)).ToArray();

            var gameData = new GameData { Categories = _cachedTags, };
            await ModioClient.DataStorage.WriteGameData(gameData);
            
            return (Error.None, _cachedTags);
        }
    }
}
