using System;

namespace Redstone.Sdk.Exceptions
{
    public class WalletServiceException : Exception
    {
        public WalletServiceException(string message) : base(message)
        {
        }
    }
}