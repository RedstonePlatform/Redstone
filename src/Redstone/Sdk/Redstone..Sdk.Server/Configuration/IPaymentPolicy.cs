using NBitcoin;
using Redstone.Sdk.Models;

namespace Redstone.Sdk.Server.Configuration
{
    public interface IPaymentPolicy
    {
        long MinimumPayment { get; set; }
        bool IsPaymentValid(Money money);
        bool IsTransactionValid(TransactionModel transaction);
    }
}