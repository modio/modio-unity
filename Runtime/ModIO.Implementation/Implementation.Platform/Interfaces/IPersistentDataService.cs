using System.Threading.Tasks;

namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for the persistent data operations.</summary>
    internal interface IPersistentDataService : IDataService
    {
        Result Initialize(long gameId, BuildSettings settings);
    }
}
