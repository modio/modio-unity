using System;
using Modio.Mods;
using Modio.Users;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyCreatorButton : ModPropertyButtonBase<UserProfile>
    {
        protected override UserProfile GetProperty(Mod mod) => mod.Creator;
    }
}
