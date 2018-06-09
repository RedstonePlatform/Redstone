namespace Redstone.Sdk.Server.Configuration
{
    public interface IRedstoneServerOptions
    {
        string PrivateKey { get; set; }
        IPaymentPolicy PaymentPolicy { get; set; }
    }
}