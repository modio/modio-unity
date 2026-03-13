using System;

namespace Modio.Mods
{
    [Flags]
    public enum GameMonetizationOptions : long
    {
        Disabled       = 0,
        Monetized      = 1,
        Marketplace    = 2,
        PartnerProgram = 4,
        LimitedStock   = 8,
        ForcedLimited  = 16,
    }
}
