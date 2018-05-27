using System.Collections.Generic;
using Newtonsoft.Json;

namespace Redstone.Sdk.Models
{
    public class WalletBuildTransactionModel
    {
        [JsonProperty(PropertyName = "fee")]
        public string Fee { get; set; }

        [JsonProperty(PropertyName = "hex")]
        public string Hex { get; set; }

        [JsonProperty(PropertyName = "transactionId")]
        public string TransactionId { get; set; }
    }

    /// <summary>
    /// A model class to be returned when the user sends a transaction successfully.
    /// </summary>
    public class WalletSendTransactionModel
    {
        /// <summary>
        /// The transaction id.
        /// </summary>
        [JsonProperty(PropertyName = "transactionId")]
        public string TransactionId { get; set; }

        /// <summary>
        /// The list of outputs in this transaction.
        /// </summary>
        [JsonProperty(PropertyName = "outputs")]
        public ICollection<TransactionOutputModel> Outputs { get; set; }
    }

    /// <summary>
    /// A simple transaction output.
    /// </summary>
    public class TransactionOutputModel
    {
        /// <summary>
        /// The output address in Base58.
        /// </summary>
        [JsonProperty(PropertyName = "address", NullValueHandling = NullValueHandling.Ignore)]
        public string Address { get; set; }

        /// <summary>
        /// The amount associated with the output.
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        public string Amount { get; set; }

        /// <summary>
        /// The data encoded in the OP_RETURN script
        /// </summary>
        [JsonProperty(PropertyName = "opReturnData", NullValueHandling = NullValueHandling.Ignore)]
        public string OpReturnData { get; set; }
    }
}