using System.Net;
using NBitcoin;
using Redstone.Features.ServiceNode.Common;

namespace Redstone.Features.ServiceNode.Models
{
    public class ServiceNodeRegistrationConfig : IServiceNodeRegistrationConfig
    {
        public RegistrationToken CreateRegistrationToken(Network network)
        {
            return new RegistrationToken(
                this.ProtocolVersion,
                this.EcdsaPubKey.GetAddress(network).ToString(),
                this.Ipv4Address,
                this.Ipv6Address,
                this.OnionAddress,
                this.ConfigurationHash,
                this.Port,
                this.EcdsaPubKey);
        }
        public int ProtocolVersion { get; set; }
        public string ServerId { get; set; }
        public IPAddress Ipv4Address { get; set; } = IPAddress.None;
        public IPAddress Ipv6Address { get; set; } = IPAddress.IPv6None;
        public string OnionAddress { get; set; }
        public int Port { get; set; }
        public Money TxOutputValue { get; set; } = new Money(1000);
        public Money TxFeeValue { get; set; } = new Money(10000);       
        public string ConfigurationHash { get; set; }
        public PubKey EcdsaPubKey { get; set; }
    }
}