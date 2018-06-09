using Redstone.Sdk.Server.Exceptions;

namespace Redstone.Sdk.Server.Configuration
{
    public class RedstoneServerOptions
    {
        public string PrivateKey { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(this.PrivateKey) /*&& check length */)
                throw new RedstoneOptionsException("PrivateKey not valid");
        }
    }
}