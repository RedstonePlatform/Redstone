using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Stratis.Bitcoin.Features.Wallet.Controllers;
using Stratis.Bitcoin.Features.Wallet.Validations;
using Stratis.Bitcoin.Utilities.ValidationAttributes;

namespace Redstone.Sdk.Models
{
    public class BuildTransactionRequest : TxFeeEstimateRequest
    {
        [MoneyFormat(isRequired: false, ErrorMessage = "The fee is not inL the correct format.")]
        public string FeeAmount { get; set; }

        [Required(ErrorMessage = "A password is required.")]
        public string Password { get; set; }

        public string OpReturnData { get; set; }
    }

    public class SendTransactionRequest : RequestModel
    {
        public SendTransactionRequest()
        {
        }

        public SendTransactionRequest(string transactionHex)
        {
            this.Hex = transactionHex;
        }

        [Required(ErrorMessage = "A transaction in hexadecimal format is required.")]
        public string Hex { get; set; }
    }

    /// <summary>
    /// Model object for <see cref="WalletController.GetTransactionFeeEstimate"/> request.
    /// </summary>
    /// <seealso cref="Stratis.Bitcoin.Features.Wallet.Models.RequestModel" />
    public class TxFeeEstimateRequest : RequestModel
    {
        [Required(ErrorMessage = "The name of the wallet is missing.")]
        public string WalletName { get; set; }

        [Required(ErrorMessage = "The name of the account is missing.")]
        public string AccountName { get; set; }

        [Required(ErrorMessage = "A destination address is required.")]
        [IsBitcoinAddress()]
        public string DestinationAddress { get; set; }

        [Required(ErrorMessage = "An amount is required.")]
        [MoneyFormat(ErrorMessage = "The amount is not in the correct format.")]
        public string Amount { get; set; }

        public string FeeType { get; set; }

        public bool AllowUnconfirmed { get; set; }

        public bool? ShuffleOutputs { get; set; }
    }

    public class RequestModel
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
