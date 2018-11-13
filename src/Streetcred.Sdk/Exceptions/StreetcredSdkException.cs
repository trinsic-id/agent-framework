using System;

namespace Streetcred.Sdk.Exceptions
{
    public class StreetcredSdkException : Exception
    {
        public ErrorCode ErrorCode { get; }

        public StreetcredSdkException(ErrorCode errorCode) : this(errorCode,
            $"An SDK errror occured. Code: {errorCode}")
        {
        }

        public StreetcredSdkException(ErrorCode errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
