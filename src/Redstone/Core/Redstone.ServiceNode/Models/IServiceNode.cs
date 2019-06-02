using NBitcoin;

namespace Redstone.ServiceNode.Models
{
    /// <summary>Interface that contains data that defines a service node.</summary>
    public interface IServiceNode
    {
        /// <summary>Public key of a service node member.</summary>
        PubKey PubKey { get; }

        /// <summary> The service nodes registration token </summary>
        RegistrationRecord RegistrationRecord { get; }

        /// <summary>Address that should have the collateral.</summary>
        string CollateralAddress { get; set; }
    }
}
