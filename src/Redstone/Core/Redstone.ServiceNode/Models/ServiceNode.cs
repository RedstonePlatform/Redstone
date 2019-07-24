using NBitcoin;
using Stratis.Bitcoin.Utilities;

namespace Redstone.ServiceNode.Models
{
    /// <summary>Representation of a service node.</summary>
    public class ServiceNode : IServiceNode
    {
        public ServiceNode(RegistrationRecord registrationRecord)
        {
            Guard.NotNull(registrationRecord, nameof(registrationRecord));

            this.RegistrationRecord = registrationRecord;
        }

        /// <inheritdoc />
        public string ServerId => this.CollateralPubKeyHash.ToString();

        /// <inheritdoc />
        public PubKey SigningPubKey => this.RegistrationRecord.Token.SigningPubKey;

        /// <inheritdoc />
        public KeyId CollateralPubKeyHash => this.RegistrationRecord.Token.CollateralPubKeyHash;

        /// <inheritdoc />
        public KeyId RewardPubKeyHash => this.RegistrationRecord.Token.RewardPubKeyHash;

        /// <inheritdoc />
        public RegistrationRecord RegistrationRecord { get; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var item = obj as ServiceNode;
            return item != null && this.RegistrationRecord.Equals(item.RegistrationRecord);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.RegistrationRecord.GetHashCode();
        }

        public static bool operator ==(ServiceNode a, ServiceNode b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if ((object)a == null || (object)b == null)
                return false;

            return a.RegistrationRecord == b.RegistrationRecord;
        }

        public static bool operator !=(ServiceNode a, ServiceNode b)
        {
            return !(a == b);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return base.ToString() + $"{nameof(this.ServerId)}:{this.ServerId ?? "null"}";
        }
    }
}
