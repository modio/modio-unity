using System;
using System.Threading.Tasks;

namespace ModIO
{
    /// <summary>
    /// This token is generated once a successful connection is established with the mod.io server
    /// and the plugin begins listening for an external authentication. You can use the provided URL
    /// and code to inform the user on how to login externally.
    /// </summary>
    /// <seealso cref="Result"/>
    /// <seealso cref="ModIOUnity.RequestExternalAuthentication"/>
    /// <seealso cref="ModIOUnityAsync.RequestExternalAuthentication"/>
    public struct ExternalAuthenticationToken
    {
        /// <summary>
        /// The URL for the user to navigate to in order to authenticate externally
        /// </summary>
        public string url;
        /// <summary>
        /// This url has the code attached so that it can automatically log in when used. This is a
        /// practical URL and not intended for display, such as a QR code or href link.
        /// </summary>
        public string autoUrl;
        /// <summary>
        /// A five digit code for the user to submit at the given url
        /// </summary>
        public string code;
        /// <summary>
        /// This can be awaited as it completes when the user has either succeeded to login
        /// externally, or code has expired or the connection has timed out
        /// </summary>
        public Task<Result> task;
        /// <summary>
        /// This is the time that the given code will expire and no longer be valid
        /// </summary>
        public DateTime expiryTime;
        
        /// <summary>
        /// This can be used to cancel the current authentication process
        /// </summary>
        /// <remarks>Note that the <see cref="task"/> will complete with a failed result</remarks>
        public void Cancel()
        {
            cancel?.Invoke();
            cancel = null;
        }

        // used and set internally
        internal Action cancel { get; set; }
    }
}
