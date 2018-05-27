using System;

namespace Redstone.Sdk.Server.Exceptions
{
    public class RequestHeaderServiceException : Exception
    {
        public RequestHeaderServiceException(string message) : base(message)
        {
        }

        public RequestHeaderServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}