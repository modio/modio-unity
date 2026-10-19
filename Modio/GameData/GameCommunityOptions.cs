using System;

namespace Modio.Mods
{
    [Flags]
    public enum GameCommunityOptions : long
    {
        None              = 0,
        EnableComments    = 1,
        EnablePreviews    = 64,
        EnablePreviewUrls = 128,
        AllowNegativeRatings = 256,
        AllowDependencies = 1024,
        DisableCarousels  = 524288,
    }
}
