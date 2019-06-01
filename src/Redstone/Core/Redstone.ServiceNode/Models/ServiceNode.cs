using NBitcoin;
using Stratis.Bitcoin.Utilities;

namespace Redstone.ServiceNode.Models
{
    /// <summary>Representation of a service node.</summary>
    public class ServiceNode : IServiceNode
    {
        public ServiceNode(RegistrationRecord registrationRecord, Money collateralAmount = null, string collateralMainchainAddress = null)
        {
            Guard.NotNull(registrationRecord, nameof(registrationRecord));

            this.RegistrationRecord = registrationRecord;
            this.CollateralAmount = collateralAmount;
            this.CollateralMainchainAddress = collateralMainchainAddress;
        }

        /// <inheritdoc />
        public PubKey PubKey => this.RegistrationRecord.Token.EcdsaPubKey;

        /// <inheritdoc />
        public RegistrationRecord RegistrationRecord { get; }

        /// <inheritdoc />
        public Money CollateralAmount { get; set; }

        /// <inheritdoc />
        public string CollateralMainchainAddress { get; set; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var item = obj as ServiceNode;
            if (item == null)
                return false;

            return this.RegistrationRecord.Equals(item.RegistrationRecord);
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

            return a.RegistrationRecord == b.RegistrationRecord && a.CollateralAmount == b.CollateralAmount && a.CollateralMainchainAddress == b.CollateralMainchainAddress;
        }

        public static bool operator !=(ServiceNode a, ServiceNode b)
        {
            return !(a == b);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return base.ToString() + $",{nameof(this.CollateralAmount)}:{this.CollateralAmount},{nameof(this.CollateralMainchainAddress)}:{this.CollateralMainchainAddress ?? "null"}";
        }
    }
}
