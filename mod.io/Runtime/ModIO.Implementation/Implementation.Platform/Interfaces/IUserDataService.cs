using System.Threading.Tasks;

namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for the user data operations.</summary>
    internal interface IUserDataService : IDataService
    {
        Task<Result> InitializeAsync(string userProfileIdentifier, long gameId,
                                     BuildSettings settings);
    }
}
