using System.Collections.Generic;
using System.Collections.ObjectModel;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Newtonsoft.Json;

namespace Modio.Mods
{

    public class ModioFeaturedContent
    {
        readonly long _id;
        readonly string _nameLocalized;
        
        internal readonly int DisplayPosition;
        
        public readonly string Name;
        
        public readonly ReadOnlyDictionary<string, string> NameLocalization;

        public string LocalizedName => NameLocalization?.GetValueOrDefault(ModioAPI.LanguageCodeResponse, _nameLocalized);
        public readonly int Total;
        public readonly ReadOnlyDictionary<string, string> Filters;
        public readonly string Size;
        public readonly bool Enabled;
        public readonly string Type;
        
        [JsonConstructor]
        public ModioFeaturedContent(long id, string name, string nameLocalized, int displayPosition, int total, ReadOnlyDictionary<string, string> filters,ReadOnlyDictionary<string,string> nameLocalization, string size, bool enabled, string type)
        {
            _id = id;
            Name = name;
            _nameLocalized = nameLocalized;
            DisplayPosition = displayPosition;
            Total = total;
            Filters = filters is null ? null : new ReadOnlyDictionary<string, string>(filters);
            NameLocalization = nameLocalization is null ? null : new ReadOnlyDictionary<string, string>(nameLocalization);
            Size = size;
            Enabled = enabled;
            Type = type;
        }
        
        public ModioFeaturedContent(PlacementObject placement)
        {
            _id = placement.Id;
            Type = placement.Type;
            Name = placement.Name;
            _nameLocalized = placement.NameLocalized;
            DisplayPosition = placement.DisplayPosition;
            Total = placement.Total;
            Filters = placement.Filters is null ? null : new ReadOnlyDictionary<string, string>(placement.Filters);
            NameLocalization = placement.NameLocalization is null ? null : new ReadOnlyDictionary<string, string>(placement.NameLocalization);
            Size = placement.Size;
            Enabled = placement.Enabled;
        }
        
    }
}
