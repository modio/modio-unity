using System;

namespace Modio.Mods
{
    [Flags]
    public enum ModCommunityOptions
    {
        None              = 0,
        EnableComments    = 1,
        EnablePreviews    = 64,
        EnablePreviewUrls = 128,
        AllowDependencies = 1024,
    }
}
