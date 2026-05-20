using System;
using Modio.Unity.UI.Components.UserProperties;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    /// <inheritdoc/>
    public class ModioUIUserProperties : ModioUIPropertiesBase<ModioUIUser, IUserProperty>
    {
        [SerializeReference] IUserProperty[] _properties = Array.Empty<IUserProperty>();
        protected override IUserProperty[] Properties => _properties;

        protected override void UpdateProperties()
        {
            if (Owner.User is null) return;
            
            foreach (IUserProperty property in _properties) property.OnUserUpdate(Owner.User);
        }
    }
}
