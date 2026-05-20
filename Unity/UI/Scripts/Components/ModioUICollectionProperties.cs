using System;
using Modio.Unity.UI.Components.ModProperties;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    /// <inheritdoc/>
    public class ModioUICollectionProperties : ModioUIPropertiesBase<ModioUICollection, ICollectionProperty>
    {
        [SerializeReference] ICollectionProperty[] _properties = Array.Empty<ICollectionProperty>();
        protected override ICollectionProperty[] Properties => _properties;

        protected override void UpdateProperties()
        {
            if (Owner.Collection == null) return;

            foreach (ICollectionProperty property in _properties) property.OnCollectionUpdate(Owner.Collection);
        }
    }
}
