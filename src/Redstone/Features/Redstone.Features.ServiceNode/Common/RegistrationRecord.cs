using System;
using NBitcoin;
using Newtonsoft.Json;

namespace Redstone.Features.ServiceNode.Common
{
    public class RegistrationRecord
    {
        public DateTime RecordTimestamp { get; set; }
        public Guid RecordGuid { get; set; }
        public string RecordTxId { get; set; }
        public string RecordTxHex { get; set; }
        public RegistrationToken Token { get; set; }
        public int BlockReceived { get; set; }

        //[JsonProperty("recordTxProof", NullValueHandling = NullValueHandling.Ignore)]
        [JsonIgnore]
        public PartialMerkleTree RecordTxProof { get; set; }

        public RegistrationRecord(DateTime recordTimeStamp, Guid recordGuid, string recordTxId, string recordTxHex, RegistrationToken record, PartialMerkleTree recordTxProof, int blockReceived = -1)
        {
            this.RecordTimestamp = recordTimeStamp;
            this.RecordGuid = recordGuid;
            this.RecordTxId = recordTxId;
            this.RecordTxHex = recordTxHex;
            this.Token = record;
            this.RecordTxProof = recordTxProof;
            this.BlockReceived = blockReceived;
        }
    }
}