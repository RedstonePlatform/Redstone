namespace Redstone.RedstoneMasterNodeD
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NBitcoin;
    using NBitcoin.Networks;
    using NBitcoin.Protocol;
    using Redstone.Core.Networks;
    using Redstone.Features.Api;
    using Redstone.Features.MasterNode;
    using Stratis.Bitcoin.Builder;
    using Stratis.Bitcoin.Configuration;
    using Stratis.Bitcoin.Features.BlockStore;
    using Stratis.Bitcoin.Features.Consensus;
    using Stratis.Bitcoin.Features.MemoryPool;
    using Stratis.Bitcoin.Features.Miner;
    using Stratis.Bitcoin.Features.RPC;
    using Stratis.Bitcoin.Features.Wallet;
    using Stratis.Bitcoin.Utilities;

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

                var node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UsePosConsensus()
                    .AddPowPosMining()
                    .UseMempool()
                    .UseWallet()
                    .UseApi()
                    .UseMasterNode()
                    .AddRPC()
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
