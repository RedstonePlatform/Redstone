using System.Net;
using NBitcoin;

namespace Redstone.ServiceNode.Models
{
    public interface IServiceNodeRegistrationConfig
    {
        int ProtocolVersion { get; set; }
        IPAddress Ipv4Address { get; set; }
        IPAddress Ipv6Address { get; set; }
        string OnionAddress { get; set; }
        int Port { get; set; }
        Money TxOutputValue { get; set; }
        Money TxFeeValue { get; set; }

        // Not sure how this works? Is ConfigurationHash is obtained after registration for checks (null for first reg)
        string ConfigurationHash { get; }

        BitcoinSecret EcdsaPrivateKey { get; set; }

        RegistrationToken CreateRegistrationToken(Network network);
    }
}