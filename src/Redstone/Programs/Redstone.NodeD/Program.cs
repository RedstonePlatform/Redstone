using System;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Protocol;
using Redstone.Core.Networks;
using Redstone.Features.Api;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
//using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.Apps;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.ColdStaking;
//using Stratis.Bitcoin.Networks;
using Stratis.Bitcoin.Utilities;

namespace Redstone.NodeD
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
//		var networkIdentifier = "test";

		var nodeSettings = new NodeSettings(networksSelector: GetNetwork(chain), protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, args: args, agent: "Redstone")
                {
                    MinProtocolVersion = ProtocolVersion.ALT_PROTOCOL_VERSION
                };

                IFullNode node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseColdStakingWallet()
                    .AddPowPosMining()
                    .UseApi()
                    .UseApps()
                    .AddRPC()
                    .Build();

                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.ToString());
            }
        }
    }
}
