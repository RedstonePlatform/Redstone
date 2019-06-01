﻿using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.ServiceNode;
using Redstone.ServiceNode.Events;
using Redstone.ServiceNode.Models;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.EventBus;
using Stratis.Bitcoin.EventBus.CoreEvents;
using Stratis.Bitcoin.Features.BlockStore.AddressIndexing;
using Stratis.Bitcoin.Signals;

namespace Redstone.Features.ServiceNode
{
    public class RegistrationScanner
    {
        public static readonly int MAX_PROTOCOL_VERSION = 128; // >128 = regard as test versions
        public static readonly int MIN_PROTOCOL_VERSION = 1;

        private readonly Network network;
        private readonly IServiceNodeManager serviceNodeManager;
        private readonly ISignals signals;
        private readonly ILogger logger;
        public readonly IAddressIndexer addressIndexer;
        private SubscriptionToken blockConnectedSubscription;

        public RegistrationScanner(
            ILoggerFactory loggerFactory,
            IServiceNodeManager serviceNodeManager,
            NodeSettings nodeSettings,
            ISignals signals,
            IAddressIndexer addressIndexer)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.network = nodeSettings.Network;
            this.serviceNodeManager = serviceNodeManager;
            this.signals = signals;

            this.addressIndexer = addressIndexer;
            this.logger.LogInformation("Initialized RegistrationManager");
        }

        public void Initialize()
        {
            this.blockConnectedSubscription = this.signals.Subscribe<BlockConnected>(this.OnBlockConnected);
        }

        public void Dispose()
        {
            this.signals.Unsubscribe(this.blockConnectedSubscription);
        }

        private void OnBlockConnected(BlockConnected blockConnected)
        {
            this.ProcessBlock(blockConnected.ConnectedBlock.ChainedHeader.Height, blockConnected.ConnectedBlock.Block);
        }

        private void ProcessBlock(int height, Block block)
        {
            this.logger.LogTrace("()");

            if (block.Transactions != null)
            {
                this.ScanForServiceNodeRegistrations(height, block);
            }

            this.logger.LogTrace("(-)");
        }

        private void ScanForServiceNodeRegistrations(int height, Block block)
        {
            foreach (Transaction tx in block.Transactions.Where(RegistrationToken.HasMarker))
            {
                this.logger.LogDebug("Received a new service node registration transaction: " + tx.GetHash());

                try
                {
                    var registrationToken = new RegistrationToken();
                    registrationToken.ParseTransaction(tx, this.network);

                    if (!registrationToken.Validate(this.network))
                    {
                        this.logger.LogDebug("Registration token failed validation");
                        continue;
                    }

                    // TODO: doesn't belong here
                    var merkleBlock = new MerkleBlock(block, new[] { tx.GetHash() });
                    var registrationRecord = new RegistrationRecord(DateTime.Now, Guid.NewGuid(), tx.GetHash().ToString(), tx.ToHex(), registrationToken, merkleBlock.PartialMerkleTree, height);

                    // Ignore protocol versions outside the accepted bounds
                    if ((registrationRecord.Token.ProtocolVersion < MIN_PROTOCOL_VERSION) ||
                        (registrationRecord.Token.ProtocolVersion > MAX_PROTOCOL_VERSION))
                    {
                        this.logger.LogDebug("Registration protocol version out of bounds " + tx.GetHash());
                        continue;
                    }

                    this.logger.LogTrace("New Service Node Registration");
                    var serviceNode = new Redstone.ServiceNode.Models.ServiceNode(registrationRecord);
                    this.serviceNodeManager.AddServiceNode(serviceNode);
                }
                catch (Exception e)
                {
                    this.logger.LogDebug("Failed to parse registration transaction, exception: " + e);
                }
            }
        }
    }
}
