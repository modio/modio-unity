#if (UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR

using System.Threading.Tasks;

namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for retrieving platform services.</summary>
    internal static partial class PlatformConfiguration
    {
#if UNITY_STANDALONE_WIN
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = "windows";
#elif UNITY_STANDALONE_OSX
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = "mac";
#elif UNITY_STANDALONE_LINUX
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = "linux";
#elif UNITY_ANDROID
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = "android";
#elif UNITY_IOS
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = "ios";
#endif
        public const bool SynchronizedDataJobs = false;

        /// <summary>Creates the user data storage service.</summary>
        public static async Task<ResultAnd<IUserDataService>> CreateUserDataService(
            string userProfileIdentifier, long gameId, BuildSettings settings)
        {
            IUserDataService service = new SystemIODataService();
            Result result = await service.InitializeAsync(userProfileIdentifier, gameId, settings);
            return ResultAnd.Create(result, service);
        }

        /// <summary>Creates the persistent data storage service.</summary>
        public static async Task<ResultAnd<IPersistentDataService>> CreatePersistentDataService(
            long gameId, BuildSettings settings)
        {
            IPersistentDataService service = new SystemIODataService();
            Result result = await service.InitializeAsync(gameId, settings);
            return ResultAnd.Create(result, service);
        }

        /// <summary>Creates the temp data storage service.</summary>
        public static async Task<ResultAnd<ITempDataService>> CreateTempDataService(
            long gameId, BuildSettings settings)
        {
            ITempDataService service = new SystemIODataService();
            Result result = await service.InitializeAsync(gameId, settings);
            return ResultAnd.Create(result, service);
        }
    }
}

#endif // UNITY_STANDALONE && !UNITY_EDITOR
