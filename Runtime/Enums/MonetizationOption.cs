
using System;

namespace ModIO
{
    /// <summary>
    /// Monetization options enabled by the creator.
    /// Multiple options can be combined.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetMods"/>
    /// <seealso cref="ModIOUnityAsync.GetMods"/>
    [Flags]
    public enum MonetizationOption
    {
        None = 0,
        Enabled = 1,
        Live = 2,
        EnablePartnerProgram = 4,
        EnableScarcity = 8,
    }
}
