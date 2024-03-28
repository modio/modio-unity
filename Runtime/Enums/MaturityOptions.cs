
namespace ModIO
{
    [System.Flags]
    public enum MaturityOptions
    {
        None     = 0b0000,
        Alcohol  = 0b0001,
        Drugs    = 0b0010,
        Violence = 0b0100,
        Explicit = 0b1000,
    }
}
