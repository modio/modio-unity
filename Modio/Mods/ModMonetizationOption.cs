using System;

namespace Modio.Mods
{
    [Flags]
    public enum ModMonetizationOption
    {
        None = 0,
        Enabled = 1,
        Live = 2,
        EnablePartnerProgram = 4,
        EnableScarcity = 8,
    }
}
