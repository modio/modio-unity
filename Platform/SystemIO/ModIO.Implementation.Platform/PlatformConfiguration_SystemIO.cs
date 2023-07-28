#if (UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WSA) && !UNITY_EDITOR

using System.Threading.Tasks;

namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for retrieving platform services.</summary>
    internal static partial class PlatformConfiguration
    {
#if UNITY_STANDALONE_WIN
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = RestApiPlatform.Windows.ToString();
#elif UNITY_WSA
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        /// UWP is not currently supported on the backend
        public static string RESTAPI_HEADER = RestApiPlatform.Windows.ToString();
#elif UNITY_STANDALONE_OSX
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = RestApiPlatform.Mac.ToString();
#elif UNITY_STANDALONE_LINUX
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = RestApiPlatform.Linux.ToString();
#elif UNITY_ANDROID
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = RestApiPlatform.Android.ToString();
#elif UNITY_IOS
        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = RestApiPlatform.Ios.ToString();
#endif
        public const bool SynchronizedDataJobs = false;

        /// <summary>Creates the user data storage service.</summary>
        public static ResultAnd<IUserDataService> CreateUserDataService(
            string userProfileIdentifier, long gameId, BuildSettings settings)
        {
            IUserDataService service = new SystemIODataService();
            Result result = service.Initialize(userProfileIdentifier, gameId, settings);
            return ResultAnd.Create(result, service);
        }

        /// <summary>Creates the persistent data storage service.</summary>
        public static ResultAnd<IPersistentDataService> CreatePersistentDataService(
            long gameId, BuildSettings settings)
        {
            IPersistentDataService service = new SystemIODataService();
            Result result = service.Initialize(gameId, settings);
            return ResultAnd.Create(result, service);
        }

        /// <summary>Creates the temp data storage service.</summary>
        public static ResultAnd<ITempDataService> CreateTempDataService(
            long gameId, BuildSettings settings)
        {
            ITempDataService service = new SystemIODataService();
            Result result = service.Initialize(gameId, settings);
            return ResultAnd.Create(result, service);
        }
    }
}

#endif // UNITY_STANDALONE && !UNITY_EDITOR
