namespace ModIO.Implementation.Platform
{
    /// <summary>Interface for retrieving platform configurations.</summary>
    internal static partial class PlatformConfiguration
    {
#if false // Never compile the following template

        // NOTE(@jackson):
        //  The following section is the template for what the platform-specific partial
        //  implementation should look like. Ensure that the following functions are included as the
        //  class is duck-typed.

        /// <summary>Holds the value for the platform header value to use in requests.</summary>
        public static string RESTAPI_HEADER = null;

        /// <summary>Creates the user data storage service.</summary>
        public static async Task<ResultAnd<IUserDataService>> CreateUserDataService(
            string userProfileIdentifier, long gameId, BuildSettings settings)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Creates the persistent data storage service.</summary>
        public static async Task<ResultAnd<IPersistentDataService>> CreatePersistentDataService(
            long gameId, BuildSettings settings)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Creates the temp data storage service.</summary>
        public static async Task<ResultAnd<ITempDataService>> CreateTempDataService(
            long gameId, BuildSettings settings)
        {
            throw new System.NotImplementedException();
        }

#endif // false
    }
}
