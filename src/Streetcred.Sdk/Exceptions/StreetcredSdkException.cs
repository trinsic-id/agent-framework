using System;
using System.Runtime.Serialization;

namespace Streetcred.Sdk.Exceptions
{
    public class StreetcredSdkException : Exception
    {
        public StreetcredSdkException()
        {
        }

        public StreetcredSdkException(string message) : base(message)
        {
        }

        public StreetcredSdkException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected StreetcredSdkException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
