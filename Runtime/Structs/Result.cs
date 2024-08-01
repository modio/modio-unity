using ResultCode = ModIO.Implementation.ResultCode;

namespace ModIO
{
    /// <summary>
    /// Struct returned from ModIO callbacks to inform the caller if the operation succeeded.
    /// </summary>
    public struct Result
    {
#region Internal Implementation

        /// <summary>Internal value of the result object.</summary>
        internal uint code;
        internal uint code_api;

#endregion // Internal Implementation

        /// <summary>
        /// A string message explaining the result error code in more detail (If one exists).
        /// </summary>
        public string message => ResultCode.GetErrorCodeMeaning(code);

        /// <summary>
        /// A string message explaining the result API error code in more detail (If one exists).
        /// </summary>
        public string apiMessage => ResultCode.GetErrorCodeMeaning(code_api);

        /// <summary>
        /// The error code for the result.
        /// 0 = Success
        /// </summary>
        public uint errorCode => code;

        /// <summary>
        /// The API reference error code for the result.
        /// 0 = No API error
        /// </summary>
        public uint apiCode => code_api;

        public bool Succeeded()
        {
            return code == ResultCode.Success;
        }

        public bool IsCancelled()
        {
            return code == ResultCode.Internal_OperationCancelled;
        }

        public bool IsInitializationError()
        {
            return code == ResultCode.Init_NotYetInitialized
                   || code == ResultCode.Init_FailedToLoadConfig;
        }

        public bool IsAuthenticationError()
        {
            return code == ResultCode.User_NotAuthenticated || code == ResultCode.User_InvalidToken
                   || code == ResultCode.User_InvalidEmailAddress
                   || code == ResultCode.User_AlreadyAuthenticated
                   || code_api == ResultCode.RESTAPI_OAuthTokenExpired;
        }

        public bool IsInvalidSecurityCode()
        {
            return code_api == ResultCode.RESTAPI_EmailExchangeCodeExpired || code_api == ResultCode.RESTAPI_EmailExchangeInvalidCode;
        }

        public bool IsInvalidEmailAddress()
        {
            return code == ResultCode.User_InvalidEmailAddress;
        }

        public bool IsPermissionError()
        {
            return this.code == 403
                   || this.code_api == ResultCode.RESTAPI_InsufficientWritePermission
                   || this.code_api == ResultCode.RESTAPI_InsufficientReadPermission
                   || this.code_api == ResultCode.RESTAPI_InsufficientCreatePermission
                   || this.code_api == ResultCode.RESTAPI_InsufficientDeletePermission;
        }

        /// <summary>
        /// Checks if the result failed due to no internet connection
        /// </summary>
        /// <returns>true if the result failed due to no internet connection</returns>
        public bool IsNetworkError()
        {
            return this.code == ResultCode.API_FailedToConnect;
        }

        public bool IsStorageSpaceInsufficient()
        {
            return this.code == ResultCode.IO_InsufficientStorage;
        }

        public bool IsRateLimited() =>
            code == ResultCode.RESTAPI_RateLimitExceededGlobal
            || code == ResultCode.RESTAPI_RateLimitExceededEndpoint;

        public override string ToString()
        {
            if (Succeeded()) return "Success";

            if(apiCode != 0)
                return $"Result({code}:{apiCode}): {message}; {apiMessage}";

            return $"Result({code}): {message}";
        }
    }
}
