using System;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Protocol;
using Redstone.Core;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Utilities;

namespace Redstone.RedstoneD
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {
            try
            {
                Network network = args.Contains("-testnet") ? RedstoneNetwork.RedstoneTest : RedstoneNetwork.RedstoneMain;
                NodeSettings nodeSettings = new NodeSettings(network, ProtocolVersion.ALT_PROTOCOL_VERSION, args:args, loadConfiguration:false);

                // NOTES: running networks side by side is not possible yet as the flags for serialization are static
                var node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    //.UsePosConsensus()
                    .UsePowConsensus() //required for pow
                    .UseBlockStore()
                    .UseMempool()
                    .UseWallet()
                    //.AddPowPosMining()
                    .AddMining() // required for pow
                    .UseApi()
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
