#if UNITY_IOS && !UNITY_EDITOR

using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for retrieving platform services.</summary>
    internal static partial class PlatformConfiguration
    {
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = "ios";

        /// <summary>Creates the user data storage service.</summary>
        public static async Task<ResultAnd<IUserDataService>> CreateUserDataService(
            string userProfileIdentifier, long gameId, BuildSettings settings)
        {
            IUserDataService service = new iOSDataService();
            Result result = await service.InitializeAsync(userProfileIdentifier, gameId, settings);
            return ResultAnd.Create(result, service);
        }

        /// <summary>Creates the persistent data storage service.</summary>
        public static async Task<ResultAnd<IPersistentDataService>> CreatePersistentDataService(
            long gameId, BuildSettings settings)
        {
            IPersistentDataService service = new iOSDataService();
            Result result = await service.InitializeAsync(gameId, settings);
            return ResultAnd.Create(result, service);
        }

        /// <summary>Creates the temp data storage service.</summary>
        public static async Task<ResultAnd<ITempDataService>> CreateTempDataService(
            long gameId, BuildSettings settings)
        {
            ITempDataService service = new iOSDataService();
            Result result = await service.InitializeAsync(gameId, settings);
            return ResultAnd.Create(result, service);
        }
    }
}

#endif
