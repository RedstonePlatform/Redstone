using System;
using System.Net;
using NBitcoin;

namespace Redstone.ServiceNode.Models
{
    public class ServiceNodeRegistrationConfig : IServiceNodeRegistrationConfig
    {
        public RegistrationToken CreateRegistrationToken(Network network)
        {
            return new RegistrationToken(
                this.ProtocolVersion,
                this.Ipv4Address,
                this.Ipv6Address,
                this.OnionAddress,
                this.ConfigurationHash,
                this.Port,
                this.CollateralPubKeyHash,
                this.RewardPubKeyHash,
                this.EcdsaPrivateKey.PubKey,
                this.ServiceEndpoint);
        }

        public int ProtocolVersion { get; set; }

        public IPAddress Ipv4Address { get; set; } = IPAddress.None;

        public IPAddress Ipv6Address { get; set; } = IPAddress.IPv6None;

        public string OnionAddress { get; set; }

        public int Port { get; set; }

        public Money TxOutputValue { get; set; } = new Money(1000);

        public Money TxFeeValue { get; set; } = new Money(10000);

        public KeyId CollateralPubKeyHash { get; set; }

        public KeyId RewardPubKeyHash { get; set; }

        public BitcoinSecret EcdsaPrivateKey { get; set; }

        public Uri ServiceEndpoint { get; set; }

        public string ConfigurationHash { get; set; }
    }
}