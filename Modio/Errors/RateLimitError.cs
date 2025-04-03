using System;

namespace Modio.Errors
{
    public enum RateLimitErrorCode : long
    {
        RATELIMITED = ErrorCode.RATELIMITED,
        RATELIMITED_SAME_ENDPOINT = 11009,
    }
    
    public class RateLimitError : Error
    {
        /// <remarks>This can be 0 in the event of a rolling rate limit.</remarks>
        public readonly int RetryAfterSeconds;

        internal RateLimitError(RateLimitErrorCode code, int retryAfterSeconds) 
            : base((ErrorCode)code) 
            => RetryAfterSeconds = retryAfterSeconds;
    }
}
