using System.Threading.Tasks;

namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for the persistent data operations.</summary>
    internal interface IPersistentDataService : IDataService
    {
        Task<Result> InitializeAsync(long gameId, BuildSettings settings);
    }
}
