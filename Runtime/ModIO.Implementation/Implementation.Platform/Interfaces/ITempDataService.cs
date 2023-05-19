namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for the temp data operations.</summary>
    internal interface ITempDataService : IDataService
    {
        Result Initialize(long gameId, BuildSettings settings);
    }
}
