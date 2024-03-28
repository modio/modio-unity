
namespace ModIO
{
    /// <summary>
    /// Finds all mods with or without a price. Default is Free.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetMods"/>
    /// <seealso cref="ModIOUnityAsync.GetMods"/>
    public enum RevenueType
    {
        Free = 0,
        Paid = 1,
        FreeAndPaid = 2
    }
}
