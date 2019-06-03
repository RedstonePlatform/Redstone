using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.ServiceNode.Models;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.EventBus;
using Stratis.Bitcoin.EventBus.CoreEvents;
using Stratis.Bitcoin.Features.BlockStore.AddressIndexing;
using Stratis.Bitcoin.Signals;

namespace Redstone.Features.ServiceNode
{
    public class ServiceNodeRegistrationChecker : IServiceNodeRegistrationChecker
    {
        private readonly Network network;
        private readonly IServiceNodeManager serviceNodeManager;
        private readonly IAddressIndexer addressIndexer;
        private readonly ISignals signals;
        private readonly ILogger logger;
        private SubscriptionToken blockConnectedSubscription;

        public ServiceNodeRegistrationChecker(
            ILoggerFactory loggerFactory,
            IServiceNodeManager serviceNodeManager,
            IAddressIndexer addressIndexer,
            NodeSettings nodeSettings,
            ISignals signals)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.network = nodeSettings.Network;
            this.serviceNodeManager = serviceNodeManager;
            this.addressIndexer = addressIndexer;
            this.signals = signals;

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
                this.CheckCollateral(height);
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

                    string collateralAddress = BitcoinAddress.Create(registrationRecord.Token.ServerId, this.network).ScriptPubKey.ToString();

                    this.logger.LogTrace("New Service Node Registration");
                    var serviceNode = new Redstone.ServiceNode.Models.ServiceNode(registrationRecord, collateralAddress);
                    this.serviceNodeManager.AddServiceNode(serviceNode);
                }
                catch (Exception e)
                {
                    this.logger.LogDebug("Failed to parse registration transaction, exception: " + e);
                }
            }
        }

        private void CheckCollateral(int height)
        {
            foreach (IServiceNode serviceNode in this.serviceNodeManager.GetServiceNodes())
            {
                try
                {
                    Money serverCollateralBalance =
                        this.addressIndexer.GetAddressBalance(serviceNode.RegistrationRecord.Token.ServerId, 1);

                    this.logger.LogDebug("Collateral balance for server " + serviceNode.RegistrationRecord.Token.ServerId + " is " +
                                         serverCollateralBalance.ToString() + ", original registration height " +
                                         serviceNode.RegistrationRecord.BlockReceived + ", current height " + height);

                    if ((serverCollateralBalance.ToUnit(MoneyUnit.BTC) < this.network.Consensus.ServiceNodeCollateralThreshold) &&
                        ((height - serviceNode.RegistrationRecord.BlockReceived) > this.network.Consensus.ServiceNodeCollateralBlockPeriod))
                    {
                        // Remove server registrations as funding has not been performed within block count,
                        // or funds have been removed from the collateral address subsequent to the
                        // registration being performed
                        this.logger.LogDebug("Insufficient collateral within window period for server: " + serviceNode.RegistrationRecord.Token.ServerId);
                        this.logger.LogDebug("Deleting registration records for server: " + serviceNode.RegistrationRecord.Token.ServerId);

                        this.serviceNodeManager.RemoveServiceNode(serviceNode);
                    }
                }
                catch (Exception e)
                {
                    this.logger.LogError("Error calculating server collateral balance: " + e);
                }
            }
        }
    }
}
