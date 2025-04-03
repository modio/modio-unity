using System;
using Modio.Mods;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyDateReleased : ModPropertyDateBase
    {
        protected override DateTime GetValue(Mod mod) => mod.DateLive;
    }
}
