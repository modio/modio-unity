using UnityEditor;

#if UNITY_EDITOR

namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for retrieving platform services.</summary>
    internal static partial class PlatformConfiguration
    {
#if UNITY_EDITOR_WIN
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = RestApiPlatform.Windows.ToString();
#elif UNITY_EDITOR_OSX
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = RestApiPlatform.Mac.ToString();
#elif UNITY_EDITOR_LINUX
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = RestApiPlatform.Linux.ToString();
#endif
        public const bool SynchronizedDataJobs = false;

        public static ResultAnd<IUserDataService> CreateUserDataService(
            string userProfileIdentifier, long gameId, BuildSettings settings)
        {
            IUserDataService service = new EditorDataService();
            Result result = service.Initialize(userProfileIdentifier, gameId, settings);
            return ResultAnd.Create(result, service);
        }

        /// <summary>Creates the persistent data storage service.</summary>
        public static ResultAnd<IPersistentDataService> CreatePersistentDataService(
            long gameId, BuildSettings settings)
        {
            IPersistentDataService service = new EditorDataService();
            Result result = service.Initialize(gameId, settings);
            return ResultAnd.Create(result, service);
        }

        /// <summary>Creates the temp data storage service.</summary>
        public static ResultAnd<ITempDataService> CreateTempDataService(
            long gameId, BuildSettings settings)
        {
            ITempDataService service = new EditorDataService();
            Result result = service.Initialize(gameId, settings);
            return ResultAnd.Create(result, service);
        }
    }
}

#endif // UNITY_EDITOR
