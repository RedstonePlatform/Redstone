using System;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Networks;
using NBitcoin.Protocol;
using Redstone.Core.Networks;
using Redstone.Features.Api;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.Dns;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Utilities;

namespace Redstone.RedstoneFullNodeDnsD
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Network network = args.Contains("-testnet")
                    ? NetworkRegistration.Register(new RedstoneTest())
                    : args.Contains("-regnet")
                    ? NetworkRegistration.Register(new RedstoneRegTest())
                    : NetworkRegistration.Register(new RedstoneMain());

                var nodeSettings = new NodeSettings(network: network, protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, args: args)
                {
                    MinProtocolVersion = ProtocolVersion.ALT_PROTOCOL_VERSION
                };

                var dnsSettings = new DnsSettings(nodeSettings);

                if (string.IsNullOrWhiteSpace(dnsSettings.DnsHostName) || string.IsNullOrWhiteSpace(dnsSettings.DnsNameServer) || string.IsNullOrWhiteSpace(dnsSettings.DnsMailBox))
                    throw new ConfigurationException("When running as a DNS Seed service, the -dnshostname, -dnsnameserver and -dnsmailbox arguments must be specified on the command line.");


                var builder = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings);

                // Run as a full node with DNS or just a DNS service?
                if (dnsSettings.DnsFullNode)
                {
                    builder = builder.UseBlockStore()
                        .UsePosConsensus()
                        .UseMempool()
                        .UseWallet();
                        .AddPowPosMining()
                }
                else
                {
                    builder = builder.UsePosConsensus();
                }

                var node = builder
                    .UseApi()
                    .AddRPC()
                    .UseDns()
                    .Build();

                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.Message);
            }
        }
    }
}
