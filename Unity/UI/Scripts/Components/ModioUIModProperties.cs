using System;
using Modio.Unity.UI.Components.ModProperties;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUIModProperties : ModioUIPropertiesBase<ModioUIMod, IModProperty>
    {
        [SerializeReference] IModProperty[] _properties = Array.Empty<IModProperty>();
        protected override IModProperty[] Properties => _properties;

        protected override void UpdateProperties()
        {
            if (Owner.Mod == null) return;

            foreach (IModProperty property in _properties) property.OnModUpdate(Owner.Mod);
        }
    }
}
