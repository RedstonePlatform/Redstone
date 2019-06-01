using System;
using System.Collections.Generic;
using NBitcoin;
using Newtonsoft.Json;
using Redstone.ServiceNode.Models;

namespace Redstone.ServiceNode.Models
{
    public class RegistrationRecord
    {
        public DateTime RecordTimestamp { get; set; }
        public Guid RecordGuid { get; set; }
        public string RecordTxId { get; set; }
        public string RecordTxHex { get; set; }
        public RegistrationToken Token { get; set; }
        public int BlockReceived { get; set; }

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

        public override bool Equals(object obj)
        {
            return obj is RegistrationRecord record &&
                   this.RecordTimestamp == record.RecordTimestamp &&
                   this.RecordGuid.Equals(record.RecordGuid) &&
                   this.RecordTxId == record.RecordTxId &&
                   this.RecordTxHex == record.RecordTxHex &&
                   EqualityComparer<RegistrationToken>.Default.Equals(this.Token, record.Token) &&
                   this.BlockReceived == record.BlockReceived &&
                   EqualityComparer<PartialMerkleTree>.Default.Equals(this.RecordTxProof, record.RecordTxProof);
        }

        public override int GetHashCode()
        {
            var hashCode = -237713240;
            hashCode = hashCode * -1521134295 + this.RecordTimestamp.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(this.RecordGuid);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.RecordTxId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.RecordTxHex);
            hashCode = hashCode * -1521134295 + EqualityComparer<RegistrationToken>.Default.GetHashCode(this.Token);
            hashCode = hashCode * -1521134295 + this.BlockReceived.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<PartialMerkleTree>.Default.GetHashCode(this.RecordTxProof);
            return hashCode;
        }
    }
}