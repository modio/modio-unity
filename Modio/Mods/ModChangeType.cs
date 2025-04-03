using System;

namespace Modio.Mods
{
    [Flags]
    public enum ModChangeType
    {
        Modfile           = 1 << 0,
        IsEnabled         = 1 << 1,
        IsSubscribed      = 1 << 2,
        ModObject         = 1 << 3,
        DownloadProgress  = 1 << 4,
        FileState         = 1 << 5,
        Rating            = 1 << 6,
        IsPurchased         = 1 << 7,
        Generic           = 1 << 8,
        Dependencies      = 1 << 9,
        Everything        = ~0,
    }
}
