using System;
using Modio.Unity.UI.Components.SearchProperties;
using Modio.Unity.UI.Search;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUISearchProperties : ModioUIPropertiesBase<ModioUISearch, ISearchProperty>
    {
        [SerializeReference] ISearchProperty[] _properties = Array.Empty<ISearchProperty>();
        protected override ISearchProperty[] Properties => _properties;

        protected override void UpdateProperties()
        {
            foreach (ISearchProperty property in _properties) property.OnSearchUpdate(Owner);
        }
    }
}
