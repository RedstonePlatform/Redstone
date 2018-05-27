using System;

namespace Redstone.Sdk.Server.Exceptions
{
    public class TokenServiceException : Exception
    {
        public TokenServiceException(string message) : base(message)
        {
        }

        public TokenServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}