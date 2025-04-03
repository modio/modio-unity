using System;
using Modio.Errors;

namespace Modio
{
    public class Error : IEquatable<Error>
    {
        public static readonly Error None = new Error(ErrorCode.NONE);
        public static readonly Error Unknown = new Error(ErrorCode.UNKNOWN);

        public readonly ErrorCode Code;

        public Error(ErrorCode code) => Code = code;

        public virtual string GetMessage() => Code.GetMessage();

        public static implicit operator bool(Error error) => error.Code != ErrorCode.NONE;
        
        //Essentially the constructor, but uses a cached None if possible
        public static explicit operator Error(ErrorCode errorCode) => errorCode == ErrorCode.NONE ? None : new Error(errorCode);

        public override string ToString() => Code == ErrorCode.NONE ? "Success" : GetMessage();

        public bool Equals(Error other) => Code == other.Code;

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is Error other && Equals(other));

        public override int GetHashCode() => Code.GetHashCode();
    }
}
