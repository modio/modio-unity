using System.Threading.Tasks;

namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for the temp data operations.</summary>
    internal interface ITempDataService : IDataService
    {
        Task<Result> InitializeAsync(long gameId, BuildSettings settings);
    }
}
