// <auto-generated />
namespace Modio.Errors
{
    public enum GenericErrorCode : long
    {
        NONE = ErrorCode.NONE,
        UNKNOWN = ErrorCode.UNKNOWN,
        /// <inheritdoc cref="ErrorCode.OPERATION_CANCELLED" />
        OPERATION_CANCELLED = ErrorCode.OPERATION_CANCELLED,
        /// <inheritdoc cref="ErrorCode.OPERATION_ERROR" />
        OPERATION_ERROR = ErrorCode.OPERATION_ERROR,
        /// <inheritdoc cref="ErrorCode.COULD_NOT_CREATE_HANDLE" />
        COULD_NOT_CREATE_HANDLE = ErrorCode.COULD_NOT_CREATE_HANDLE,
        /// <inheritdoc cref="ErrorCode.NO_DATA_AVAILABLE" />
        NO_DATA_AVAILABLE = ErrorCode.NO_DATA_AVAILABLE,
        /// <inheritdoc cref="ErrorCode.END_OF_FILE" />
        END_OF_FILE = ErrorCode.END_OF_FILE,
        /// <inheritdoc cref="ErrorCode.QUEUE_CLOSED" />
        QUEUE_CLOSED = ErrorCode.QUEUE_CLOSED,
        /// <inheritdoc cref="ErrorCode.SDKALREADY_INITIALIZED" />
        SDKALREADY_INITIALIZED = ErrorCode.SDKALREADY_INITIALIZED,
        /// <inheritdoc cref="ErrorCode.SDKNOT_INITIALIZED" />
        SDKNOT_INITIALIZED = ErrorCode.SDKNOT_INITIALIZED,
        /// <inheritdoc cref="ErrorCode.INDEX_OUT_OF_RANGE" />
        INDEX_OUT_OF_RANGE = ErrorCode.INDEX_OUT_OF_RANGE,
        /// <inheritdoc cref="ErrorCode.BAD_PARAMETER" />
        BAD_PARAMETER = ErrorCode.BAD_PARAMETER,
        /// <inheritdoc cref="ErrorCode.SHUTTING_DOWN" />
        SHUTTING_DOWN = ErrorCode.SHUTTING_DOWN,
        /// <inheritdoc cref="ErrorCode.MISSING_COMPONENTS" />
        MISSING_COMPONENTS = ErrorCode.MISSING_COMPONENTS,
    }

    public class GenericError : Error
    {
        public static readonly new GenericError None = new GenericError(GenericErrorCode.NONE);

        public new GenericErrorCode Code => (GenericErrorCode)base.Code;

        public GenericError(GenericErrorCode code) : base((ErrorCode)code){ }
    }

    public static partial class ErrorExtensions
    {
        public static string GetMessage(this GenericErrorCode errorCode, string append = null) => GetMessage((ErrorCode)errorCode, append);
    }
}
