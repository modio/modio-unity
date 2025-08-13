using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Modio.Errors;

namespace Modio
{
    public class Error : IEquatable<Error>
    {
        public static bool StoreStackTraceWhenCreated;
        
        public static readonly Error None = new Error(ErrorCode.NONE);
        public static readonly Error Unknown = new Error(ErrorCode.UNKNOWN);

        public readonly ErrorCode Code;
        readonly StackTrace _stackTrace;
        protected List<(string memberName, string sourceFilePath, int sourceLineNumber, string message)>
            _callInformation;

        public Error(ErrorCode code)
        {
            Code = code;

            if (StoreStackTraceWhenCreated)
                _stackTrace = new StackTrace(1, true);
        }

        /// <summary>If an error is silent, don't print an error to the console.</summary>
        public bool IsSilent => Code is ErrorCode.SHUTTING_DOWN or ErrorCode.OPERATION_CANCELLED;

        public virtual string GetMessage()
        {
            if (_stackTrace != null)
                return $"{Code.GetMessage()}\n at:\n{_stackTrace}";
            if(_callInformation != null)
            {
                string formattedCall = string.Join('\n', _callInformation.Select(
                                                       a => $"{a.sourceFilePath}: {a.memberName}:{a.sourceLineNumber}:{a.message}"));
                return $"{Code.GetMessage()}\n at:\n{formattedCall}";
            }

            return Code.GetMessage();
        }

        public static implicit operator bool(Error error) => error.Code != ErrorCode.NONE;
        
        //Essentially the constructor, but uses a cached None if possible
        public static explicit operator Error(ErrorCode errorCode) => errorCode == ErrorCode.NONE ? None : new Error(errorCode);

        public override string ToString() => Code == ErrorCode.NONE ? "Success" : GetMessage();

        public bool Equals(Error other) => Code == other.Code;

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is Error other && Equals(other));

        public override int GetHashCode() => Code.GetHashCode();

        public Error AddMethodContext(
            string message = null,
            [System.Runtime.CompilerServices.CallerMemberName]
            string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath]
            string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber]
            int sourceLineNumber = 0
        )
        {
            //don't do anything if it's a cached NONE object
            if (Code == ErrorCode.NONE)
                return this;
            
            _callInformation ??= new List<(string memberName, string sourceFilePath, int sourceLineNumber, string message)>();
            
            _callInformation.Add((memberName, sourceFilePath, sourceLineNumber, message));
            
            return this;
        }
    }

}
