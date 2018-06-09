using Redstone.Sdk.Server.Exceptions;

namespace Redstone.Sdk.Server.Configuration
{
    public class RedstoneServerOptions : IRedstoneServerOptions
    {
        public RedstoneServerOptions(IPaymentPolicy paymentPolicy)
        {
            PaymentPolicy = paymentPolicy;
        }

        public string PrivateKey { get; set; }
        public int RequiredConfirmations { get; set; } = 1;
        public IPaymentPolicy PaymentPolicy { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(this.PrivateKey) /*&& check length */)
                throw new RedstoneOptionsException("PrivateKey not valid");
        }
    }
}