using System;

namespace Modio.Mods
{
    [Flags]
    public enum ModMaturityOptions
    {
        None     = 0,
        Alcohol  = 1,
        Drugs    = 2,
        Violence = 4,
        Explicit = 8,
    }
}
