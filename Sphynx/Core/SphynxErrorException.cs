namespace Sphynx.Core
{
    public class SphynxErrorException : Exception
    {
        public SphynxErrorCode ErrorCode { get; }

        public SphynxErrorException(SphynxErrorCode errorCode) : this(errorCode, $"Error code 0x{(int)errorCode:X}: {errorCode.ToString()}")
        {
        }

        public SphynxErrorException(SphynxErrorCode errorCode, string? message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public SphynxErrorException(SphynxErrorCode errorCode, string? message, Exception? innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
