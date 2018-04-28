using System;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin.Protocol;
using Redstone.Core;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.LightWallet;
using Stratis.Bitcoin.Features.Notifications;
using Stratis.Bitcoin.Utilities;

namespace Redstone.RedstoneSpvD
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
                const string agent = "RedstoneSpv";

                var isTestNet = args.Contains("-testnet");
                var network = isTestNet ? RedstoneNetwork.RedstoneTest : RedstoneNetwork.RedstoneMain;            

                var nodeSettings = new NodeSettings(network, ProtocolVersion.ALT_PROTOCOL_VERSION, agent, loadConfiguration:false);

                var node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseLightWallet()
                    .UseBlockNotification()
                    .UseTransactionNotification()
                    .UseApi()
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
