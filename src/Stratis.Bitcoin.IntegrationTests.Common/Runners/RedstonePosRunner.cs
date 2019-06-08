using NBitcoin;
using NBitcoin.Protocol;
using Redstone.Features.ServiceNode;
using Stratis.Bitcoin.Base;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.Notifications;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.WatchOnlyWallet;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Stratis.Bitcoin.P2P;

namespace Stratis.Bitcoin.IntegrationTests.Common.Runners
{
    public sealed class RedstonePosRunner : NodeRunner
    {
        public RedstonePosRunner(string dataDir, Network network, string agent = "Redstone")
            : base(dataDir, agent)
        {
            this.Network = network;
        }

        public override void BuildNode()
        {
            var settings = new NodeSettings(this.Network, 
                ProtocolVersion.PROVEN_HEADER_VERSION, 
                this.Agent, 
                args: new string[] { "-savetrxhex=1", "-txIndex=1", "-addressIndex=1", "-conf=redstone.conf", $"-datadir={this.DataFolder}" });

            var builder = new FullNodeBuilder()
                .UseNodeSettings(settings)
                .UseBlockStore()
                .UsePosConsensus()
                .UseMempool()
                .UseBlockNotification()
                .UseTransactionNotification()
                .UseWallet()
                .UseWatchOnlyWallet()
                .AddServiceNodeRegistration()
                .AddRedstoneMining()
                .AddRPC()
                .UseApi()
                .UseTestChainedHeaderTree()
                .MockIBD();

            if (this.OverrideDateTimeProvider)
                builder.OverrideDateTimeProviderFor<MiningFeature>();

            if (!this.EnablePeerDiscovery)
            {
                builder.RemoveImplementation<PeerConnectorDiscovery>();
                builder.ReplaceService<IPeerDiscovery, BaseFeature>(new PeerDiscoveryDisabled());
            }

            this.FullNode = (FullNode)builder.Build();
        }
    }
}