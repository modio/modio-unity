using System;
using System.Linq;
using System.Threading.Tasks;
using Modio.API.SchemaDefinitions;
using Modio.Extensions;
using Modio.Settings;
using Newtonsoft.Json;

namespace Modio.Mods
{
    public class GameTagCategory
    {
        static GameTagCategory[] _cachedTags;
        static GameTagCategory[] _cachedCollectionTags;
        static Task<(Error, GameTagCategory[])> _cachedGetTags;

        public readonly string Name;
        public readonly bool MultiSelect;
        public readonly ModTag[] Tags; 
        public readonly bool Locked;

        [JsonProperty]
        bool _hidden;
        /// <summary>
        /// Should this tag category be hidden from users in all UI?
        /// Set on the mod.io website when configuring your game
        /// </summary>
        [JsonIgnore]
        public bool Hidden
        {
            get => _hidden || TempHidden;
            internal set => _hidden = value;
        }
        
        /// <summary>
        /// Should this tag category be hidden for the user temporarily
        /// We use this when filtering the entire mods page for a particular search
        ///
        /// e.g. you want to have separate "maps" and "characters" pages using a tag filter,
        /// but don't want the tags to be visible on those pages
        /// </summary>
        [JsonIgnore]
        public bool TempHidden { private get; set; }        

        [JsonConstructor]
        internal GameTagCategory(string name, bool multiSelect, ModTag[] tags, bool _hidden, bool locked)
        {
            Name = name;
            MultiSelect = multiSelect;
            Tags = tags;
            this._hidden = _hidden;
            Locked = locked;

            if (ModioClient.Settings.TryGetPlatformSettings(out ModioHiddenTagOverrideSettings tagSettings)
                && tagSettings.HideTagCategories.Contains(Name))
                Hidden = true;
        }
        
        internal GameTagCategory(GameTagOptionObject tagObject){
            Name = tagObject.Name;
            MultiSelect = tagObject.Type == "checkboxes";
            Hidden = tagObject.Hidden;
            Locked = tagObject.Locked;
            Tags = tagObject.Tags.Select(tagName => ModTag.Get(tagName)).ToArray();
            
            if (ModioClient.Settings.TryGetPlatformSettings(out ModioHiddenTagOverrideSettings tagSettings)
                && tagSettings.HideTagCategories.Contains(Name))
                Hidden = true;
            
            foreach ((string tagName, int count) in tagObject.TagCountMap)
            {
                ModTag tag = ModTag.Get(tagName);
                tag.Count = count;
                tag.IsVisible = !Hidden;
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
            _cachedGetTags ??= GetGameTagOptionsInternal();
            (Error, GameTagCategory[]) getTagsResult = await _cachedGetTags;
            _cachedGetTags = null;
            return getTagsResult;
        }
        public static async Task<(Error, GameTagCategory[])> GetGameTagOptionsInternal()
        {
            if (_cachedTags != null) return (Error.None, _cachedTags);
            
            (Error error, Pagination<GameTagOptionObject[]>? gameTagOptionObjects) = await API.ModioAPI.Tags.GetGameTagOptions();
            
            if (error)
            {
                (Error readCacheError, GameData cachedGameData) = await GameData.GetGameDataFromDisk();
                
                if (readCacheError)
                {
                    //Note we return the web error, not the cache error, as that's more useful
                    return (error, Array.Empty<GameTagCategory>());
                }
                
                _cachedTags = cachedGameData.Categories;
                return (Error.None, _cachedTags);
            }

            _cachedTags = gameTagOptionObjects.Value.Data.Select(options => new GameTagCategory(options)).ToArray();
            
            GameData.SetGameTags(_cachedTags).ForgetTaskSafely();
            
            return (Error.None, _cachedTags);
        }

        /// <summary>
        /// Get the tags which are usable for collections. These are hardcoded
        /// and not changeable per game.
        /// </summary>
        public static Task<(Error, GameTagCategory[])> GetCollectionTagOptions()
        {
            if (_cachedCollectionTags != null)
                return Task.FromResult((Error.None, _cachedCollectionTags));

            _cachedCollectionTags = new[]
            {
                new GameTagCategory(
                    "Category",
                    false,
                    new[]
                    {
                        ModTag.Get("Miscellaneous", ResourceTagType.CollectionCategory),
                        ModTag.Get("Essential", ResourceTagType.CollectionCategory),
                        ModTag.Get("Themed", ResourceTagType.CollectionCategory),
                    },
                    false,
                    false
                ),
                new GameTagCategory(
                    "Tags",
                    true,
                    new[]
                    {
                        ModTag.Get("Animation", ResourceTagType.CollectionTag),
                        ModTag.Get("Audio", ResourceTagType.CollectionTag),
                        ModTag.Get("Bug Fixes", ResourceTagType.CollectionTag),
                        ModTag.Get("Cheating", ResourceTagType.CollectionTag),
                        ModTag.Get("Environment", ResourceTagType.CollectionTag),
                        ModTag.Get("Gameplay", ResourceTagType.CollectionTag),
                        ModTag.Get("Quality of Life", ResourceTagType.CollectionTag),
                        ModTag.Get("UI", ResourceTagType.CollectionTag),
                        ModTag.Get("Visuals", ResourceTagType.CollectionTag),
                    },
                    false,
                    false
                ),
            };

            foreach (GameTagCategory category in _cachedCollectionTags)
                foreach (ModTag tag in category.Tags)
                    tag.IsVisible = true;

            return Task.FromResult((Error.None, _cachedCollectionTags));
        }
    }
}
