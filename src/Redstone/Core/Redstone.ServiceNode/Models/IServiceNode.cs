using NBitcoin;

namespace Redstone.ServiceNode.Models
{
    /// <summary>Interface that contains data that defines a service node.</summary>
    public interface IServiceNode
    {
        /// <summary>Public key of a service node member.</summary>
        PubKey SigningPubKey { get; }

        /// <summary>Public key hash of a service node collateral.</summary>
        KeyId CollateralPubKeyHash { get; }

        /// <summary>Public key hash for a service node reward.</summary>
        KeyId RewardPubKeyHash { get; }

        /// <summary> The service nodes registration token </summary>
        RegistrationRecord RegistrationRecord { get; }

        /// <summary>Address that should have the collateral.</summary>
        string CollateralAddress { get; set; }
    }
}
