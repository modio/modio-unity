using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Modio.Errors
{
    public class ErrorException : Error
    {
        public readonly Exception Exception;

        public override string GetMessage() => $"{base.GetMessage()}: {Exception}";
        
        internal ErrorException(Exception exception, ErrorCode code) : base(code) => Exception = exception;
        internal ErrorException(Exception exception) : base(ErrorCodeFromException(exception)) => Exception = exception;
        
        static ErrorCode ErrorCodeFromException(Exception exception) => exception switch
        {
            UnauthorizedAccessException _ => ErrorCode.NO_PERMISSION,
            DirectoryNotFoundException _  => ErrorCode.DIRECTORY_NOT_FOUND,
            FileNotFoundException _       => ErrorCode.FILE_NOT_FOUND,
            HttpRequestException _        => ErrorCode.HTTP_EXCEPTION,
            TaskCanceledException _       => ErrorCode.OPERATION_CANCELLED,
            _                             => ErrorCode.OPERATION_ERROR,
        };
    }
}
