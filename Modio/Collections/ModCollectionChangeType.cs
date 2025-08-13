using System;

namespace Modio.Collections
{
    [Flags]
    public enum ModCollectionChangeType
    {
        IsFollowed         = 1 << 0,
        Rating            = 1 << 1,
        ModList          = 1 << 2,
        Everything        = ~0,

        
    }
}
