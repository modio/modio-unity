using System;
using Modio.Mods;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyModButton : ModPropertyButtonBase<Mod>
    {
        protected override Mod GetProperty(Mod mod) => mod;
    }
}
