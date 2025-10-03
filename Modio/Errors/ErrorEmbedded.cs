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

        internal ErrorEmbedded(ErrorObject.EmbeddedError error) : this((ErrorCode)error.Code, error)
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
        
    }
}
