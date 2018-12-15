using System;

namespace AgentFramework.Core.Exceptions
{
    public class AgentFrameworkException : Exception
    {
        public ErrorCode ErrorCode { get; }

        public AgentFrameworkException(ErrorCode errorCode) : this(errorCode,
            $"Framework error occured. Code: {errorCode}")
        {
        }

        public AgentFrameworkException(ErrorCode errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public AgentFrameworkException(ErrorCode errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
