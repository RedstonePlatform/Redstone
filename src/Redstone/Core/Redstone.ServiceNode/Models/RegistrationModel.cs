using System;
using NBitcoin;
using Newtonsoft.Json;
using Stratis.Bitcoin.Features.Wallet;

namespace Redstone.ServiceNode.Models
{
    public class RegistrationModel
    {
        public DateTime RecordTimestamp { get; set; }

        public string RecordTxId { get; set; }

        public string RecordTxHex { get; set; }

        public int BlockReceived { get; set; }

        public string ServerId { get; set; }
    }
}
