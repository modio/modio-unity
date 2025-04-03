using System;

namespace Modio.Mods.Builder
{
    [Flags]
    public enum ChangeFlags
    {
        None               = 0,
        Name               = 1 << 0,
        Summary            = 1 << 1,
        Description        = 1 << 2,
        Logo               = 1 << 3,
        Gallery            = 1 << 4,
        Tags               = 1 << 5,
        MetadataBlob       = 1 << 6,
        MetadataKvps       = 1 << 7,
        Visibility         = 1 << 8,
        MaturityOptions    = 1 << 9,
        CommunityOptions   = 1 << 10,
        Modfile            = 1 << 11,
        MonetizationConfig = 1 << 12,
        Dependencies       = 1 << 13,
        
        AddFlags = Name
                   | Summary
                   | Description
                   | Logo
                   | Visibility
                   | MaturityOptions
                   | CommunityOptions
                   | MetadataBlob
                   | Tags,
        EditFlags = Name
                    | Summary
                    | Description
                    | Logo
                    | Visibility
                    | MaturityOptions
                    | CommunityOptions
                    | MetadataBlob
                    | Tags
                    | MonetizationConfig,
    }
}
