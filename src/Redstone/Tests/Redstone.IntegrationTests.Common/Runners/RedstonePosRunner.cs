﻿using NBitcoin;
using NBitcoin.Protocol;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Notifications;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.WatchOnlyWallet;
using Redstone.Features.ServiceNode;

namespace Redstone.IntegrationTests.Common.Runners
{
    public sealed class RedstonePosRunner : NodeRunner
    {
        public RedstonePosRunner(string dataDir, Network network)
            : base(dataDir)
        {
            this.Network = network;
        }

        public override void BuildNode()
        {
            var settings = new NodeSettings(this.Network, ProtocolVersion.ALT_PROTOCOL_VERSION, args: new string[] { "-conf=stratis.conf", "-datadir=" + this.DataFolder });

            this.FullNode = (FullNode)new FullNodeBuilder()
                .UseNodeSettings(settings)
                .UsePosConsensus()
                .UseBlockStore()
                .UseMempool()
                .UseBlockNotification()
                .UseTransactionNotification()
                .UseWallet()
                .UseWatchOnlyWallet()
                .AddPowPosMining()
                .AddRPC()
                //.UseApi()
                .MockIBD()
                //.SubstituteDateTimeProviderFor<MiningFeature>()   
                .UseServiceRegistration()
                .Build();
        }

        //CoreNode node2 = builder.CreateStratisPosNode(true, fullNodeBuilder =>
        //{
        //    fullNodeBuilder
        //        .UsePosConsensus()-
        //        .UseBlockStore()-
        //        .UseMempool()-
        //        .UseBlockNotification()-
        //        .UseTransactionNotification()
        //        .UseWallet()-
        //        .UseWatchOnlyWallet()x
        //        .AddPowPosMining()-
        //        //.AddMining()
        //        //.UseApi()
        //        .AddRPC()-
        //        .UseRegistration();
        //});


        /// <summary>
        /// Builds a node with POS miner and RPC enabled.
        /// </summary>
        /// <param name="dataDir">Data directory that the node should use.</param>
        /// <returns>Interface to the newly built node.</returns>
        /// <remarks>Currently the node built here does not actually stake as it has no coins in the wallet,
        /// but all the features required for it are enabled.</remarks>
        public static IFullNode BuildStakingNode(string dataDir, bool staking = true)
        {
            var nodeSettings = new NodeSettings(protocolVersion: ProtocolVersion.ALT_PROTOCOL_VERSION, args: new string[] { $"-datadir={dataDir}", $"-stake={(staking ? 1 : 0)}", "-walletname=dummy", "-walletpassword=dummy" });
            var fullNodeBuilder = new FullNodeBuilder(nodeSettings);
            IFullNode fullNode = fullNodeBuilder
                                .UseBlockStore()
                                .UsePosConsensus()
                                .UseMempool()
                                .UseWallet()
                                .AddPowPosMining()
                                .AddRPC()
                                .MockIBD()
                                .Build();

            return fullNode;
        }
    }
}