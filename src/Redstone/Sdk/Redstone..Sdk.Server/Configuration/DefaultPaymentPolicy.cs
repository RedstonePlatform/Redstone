using NBitcoin;
using Redstone.Sdk.Models;

namespace Redstone.Sdk.Server.Configuration
{
    public class DefaultPaymentPolicy : IPaymentPolicy
    {
        public long MinimumPayment { get; set; } = 1;
        public long RequiredConfirmations { get; set; } = 1;

        public bool IsPaymentValid(Money money)
        {
            return MinimumPayment <= money?.Satoshi;
        }

        // TODO : need better naming
        public bool IsTransactionValid(TransactionModel transaction)
        {
            return RequiredConfirmations <= transaction?.Confirmations;
        }
    }
}