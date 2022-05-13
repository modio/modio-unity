
using System;
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

        public bool Succeeded()
        {
            return code == ResultCode.Success;
        }

        public bool IsCancelled()
        {
            return code == ResultCode.Internal_OperationCancelled;
        }

        public bool IsInitialisationError()
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
            return code == ResultCode.RESTAPI_11012 || code == ResultCode.RESTAPI_11014;
        }

        public bool IsPermissionError()
        {
            return this.code == 403
                   || this.code_api == ResultCode.RESTAPI_InsufficientWritePermission
                   || this.code_api == ResultCode.RESTAPI_InsufficientReadPermission
                   || this.code_api == ResultCode.RESTAPI_InsufficientCreatePermission
                   || this.code_api == ResultCode.RESTAPI_InsufficientDeletePermission;
        }

        public bool IsNetworkError()
        {
            throw new NotImplementedException();
        }

        public bool IsHttpError()
        {
            throw new NotImplementedException();
        }
    }
}
