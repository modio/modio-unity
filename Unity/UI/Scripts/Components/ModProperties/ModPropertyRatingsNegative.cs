using System;
using Modio.Mods;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyRatingsNegative : ModPropertyNumberBase
    {
        protected override long GetValue(Mod mod) => mod.Stats.RatingsNegative;
    }
}
