using System;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Networks;
using NBitcoin.Protocol;
using Redstone.Core.Networks;
using Redstone.Features.Api;
using Redstone.Features.BlockExplorer;
using Redstone.Features.ServiceNode;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Apps;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.ColdStaking;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.Dns;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.Notifications;
using Stratis.Bitcoin.Features.WatchOnlyWallet;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Utilities;
using Redstone.ServiceNode.Consensus;
using Redstone.ServiceNode.Utils;
using System.Text;

namespace Redstone.RedstoneServiceNodeD
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

                var nodeSettings = new NodeSettings(network: network, protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, args: args.Concat(new[] { "-txIndex=1", "-addressIndex=1" }).ToArray())
                {
                    MinProtocolVersion = ProtocolVersion.ALT_PROTOCOL_VERSION
                };

                bool keyGenerationRequired = RequiresKeyGeneration(nodeSettings);
                if (keyGenerationRequired)
                {
                    GenerateServiceNodeKey(nodeSettings);
                    return;
                }

                var dnsSettings = new DnsSettings(nodeSettings);

                var isDns = !string.IsNullOrWhiteSpace(dnsSettings.DnsHostName) &&
                            !string.IsNullOrWhiteSpace(dnsSettings.DnsNameServer) &&
                            !string.IsNullOrWhiteSpace(dnsSettings.DnsMailBox);

                var builder = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings);

                if (isDns)
                {
                    // Run as a full node with DNS or just a DNS service?
                    if (dnsSettings.DnsFullNode)
                    {
                        builder = builder
                            .UseBlockStore()
                            .UseRedstonePosConsensus()
                            .UseMempool()
                            .UseWallet()
                            .AddRedstoneMining();
                    }
                    else
                    {
                        builder = builder.UsePosConsensus();
                    }

                    builder = builder
                        .UseApi()
                        .AddRPC()
                        .UseDns();
                }
                else
                {
                    builder = builder
                       .UseBlockStore()
                       .UseRedstonePosConsensus()
                       .UseMempool()
                       .UseColdStakingWallet()
                       .AddRedstoneMining()
                       .UseApi()
                       .UseWatchOnlyWallet()
                       .UseBlockNotification()
                       .UseTransactionNotification()
                       .AddServiceNodeRegistration()
                       .UseApps()
                       .UseBlockExplorer()
                       .AddRPC();
                }

                var node = builder.Build();

                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.ToString());
            }
        }

        private static bool RequiresKeyGeneration(NodeSettings nodeSettings)
        {
            bool generateSetting = nodeSettings.ConfigReader.GetOrDefault("generateKeyPair", false);
            if (generateSetting)
                return true;

            var tool = new KeyTool(nodeSettings.DataFolder);
            if (tool.LoadPrivateKey() == null)
                return true;

            return false;
        }
        private static void GenerateServiceNodeKey(NodeSettings nodeSettings)
        {
            var tool = new KeyTool(nodeSettings.DataFolder);
            Key key = tool.GeneratePrivateKey();

            string savePath = tool.GetPrivateKeySavePath();
            tool.SavePrivateKey(key);

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"ServiceNode key pair was generated and saved to {savePath}.");
            stringBuilder.AppendLine("Make sure to back it up!");
            stringBuilder.AppendLine($"Your public key is {key.PubKey}.");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("You can now restart the node to use the key pair. If you need to re-generate add generateKeyPair to settings");
            stringBuilder.AppendLine("Press eny key to exit...");

            Console.WriteLine(stringBuilder.ToString());

            Console.ReadKey();
        }
    }
}
