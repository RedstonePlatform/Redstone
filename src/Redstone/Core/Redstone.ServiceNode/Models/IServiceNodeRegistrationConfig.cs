using System;
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

        KeyId CollateralPubKeyHash { get; set; }

        KeyId RewardPubKeyHash { get; set; }

        BitcoinSecret EcdsaPrivateKey { get; set; }

        Uri ServiceEndpoint { get; set; }

        // Not sure how this works? Is ConfigurationHash is obtained after registration for checks (null for first reg)
        string ConfigurationHash { get; }

        RegistrationToken CreateRegistrationToken(Network network);
    }
}