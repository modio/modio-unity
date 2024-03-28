using System;

namespace ModIO
{
    [Flags]
    public enum GameMonetizationOptions
    {
        All                  = 0b0000,
        Enabled              = 0b0001,
        EnableMarketplace    = 0b0010,
        EnablePartnerProgram = 0b0100,
        EnableScarcity       = 0b1000
    }
}
