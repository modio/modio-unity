namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for the user data operations.</summary>
    internal interface IUserDataService : IDataService
    {
        Result Initialize(string userProfileIdentifier, long gameId,
            BuildSettings settings);
    }
}
