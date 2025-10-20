using System.Linq;
using Modio.API.SchemaDefinitions;
using Modio.Errors;

namespace Modio
{
    public class ErrorEmbedded : Error
    {
        public string Message;
        public string Errors;
        
        internal ErrorEmbedded(ErrorCode code) : base(code)
        {
        }

        internal ErrorEmbedded(ErrorObject.EmbeddedError error) : this((ErrorCode)error.ErrorRef, error)
        {
        }
        
        internal ErrorEmbedded(ErrorCode code, ErrorObject.EmbeddedError error) : base(code)
        {
            Message = error.Message;
            Errors = error.Errors?.ToString();
        }
        
        /// <summary>
        /// Create an embedded error from an existing error and an embedded error object
        /// </summary>
        /// <param name="error"></param>
        /// <param name="embeddedError"></param>
        internal ErrorEmbedded(Error error, ErrorObject.EmbeddedError embeddedError) : this(error.Code, embeddedError)
        {
        }

        /// <summary>
        /// Essentially the same logic as Error.GetMessage, but also includes the Message and validation Errors from the server
        /// </summary>
        public override string GetMessage()
        {
            if (_stackTrace != null)
                return $"{Code.GetMessage()}\n{Message}\n{Errors}\n at:\n{_stackTrace}";
            if(_callInformation != null)
            {
                string formattedCall = string.Join('\n', _callInformation.Select(
                                                       a => $"{a.sourceFilePath}: {a.memberName}:{a.sourceLineNumber}:{a.message}"));
                return $"{Code.GetMessage()}\n{Message}\n{Errors}\n at:\n{formattedCall}";
            }

            return $"{Code.GetMessage()}\n{Message}\n{Errors}";
        }
    }
}
