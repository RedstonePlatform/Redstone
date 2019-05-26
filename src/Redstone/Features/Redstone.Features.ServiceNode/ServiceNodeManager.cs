using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.Features.ServiceNode.Common;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.EventBus;
using Stratis.Bitcoin.EventBus.CoreEvents;
using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.BlockStore.AddressIndexing;
using Stratis.Bitcoin.Features.BlockStore.Controllers;
using Stratis.Bitcoin.Signals;

namespace Redstone.Features.ServiceNode
{
    public class ServiceNodeManager : IServiceNodeManager
    {
        public static readonly int MAX_PROTOCOL_VERSION = 128; // >128 = regard as test versions
        public static readonly int MIN_PROTOCOL_VERSION = 1;

        private readonly RegistrationStore registrationStore;
        private readonly Network network;
        private readonly ISignals signals;
        private readonly ILogger logger;
        //private readonly IBlockStoreClient blockStoreClient;
        public readonly IAddressIndexer addressIndexer;
        private SubscriptionToken blockConnectedSubscription;

        public ServiceNodeManager(
            ILoggerFactory loggerFactory,
            NodeSettings nodeSettings,
            RegistrationStore registrationStore,
            ISignals signals,
            IAddressIndexer addressIndexer)
        //    IHttpClientFactory httpClientFactory, 
        //    ApiSettings settings)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.registrationStore = registrationStore;
            this.network = nodeSettings.Network;
            this.signals = signals;

            // swap for block store client when separating out node
            this.addressIndexer = addressIndexer;
            //this.blockStoreClient = new BlockStoreClient(loggerFactory, httpClientFactory, settings.ApiPort);
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

        public RegistrationStore GetRegistrationStore()
        {
            return this.registrationStore;
        }

        private void ProcessBlock(int height, Block block)
        {
            this.logger.LogTrace("()");

            // Check for any server registration transactions
            if (block.Transactions != null)
            {
                this.CheckForServiceNodeRegistrations(height, block);
                this.CheckCollateral(height);
            }

            this.logger.LogTrace("(-)");
        }

        private void CheckForServiceNodeRegistrations(int height, Block block)
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

                    var merkleBlock = new MerkleBlock(block, new[] { tx.GetHash() });
                    var registrationRecord = new RegistrationRecord(DateTime.Now, Guid.NewGuid(), tx.GetHash().ToString(), tx.ToHex(), registrationToken, merkleBlock.PartialMerkleTree, height);

                    // Ignore protocol versions outside the accepted bounds
                    if ((registrationRecord.Token.ProtocolVersion < MIN_PROTOCOL_VERSION) ||
                        (registrationRecord.Token.ProtocolVersion > MAX_PROTOCOL_VERSION))
                    {
                        this.logger.LogDebug("Registration protocol version out of bounds " + tx.GetHash());
                        continue;
                    }

                    // If there were other registrations for this server previously, remove them and add the new one                         
                    this.logger.LogTrace("Registrations - AddWithReplace");

                    this.registrationStore.AddWithReplace(registrationRecord);

                    this.logger.LogTrace("Registration transaction for server collateral address: " + registrationRecord.Token.ServerId);
                    this.logger.LogTrace("Server Onion address: " + registrationRecord.Token.OnionAddress);
                    this.logger.LogTrace("Server configuration hash: " + registrationRecord.Token.ConfigurationHash);
                }
                catch (Exception e)
                {
                    this.logger.LogDebug("Failed to parse registration transaction, exception: " + e);
                }
            }
        }

        private void CheckCollateral(int height)
        {
            foreach (RegistrationRecord registrationRecord in this.registrationStore.GetAll())
            {
                try
                {
                    Money serverCollateralBalance =
                        //await this.blockStoreClient.GetAddressBalanceAsync(registrationRecord.Token.ServerId, 1);
                        this.addressIndexer.GetAddressBalance(registrationRecord.Token.ServerId, 1) ?? new Money(0);

                    this.logger.LogDebug("Collateral balance for server " + registrationRecord.Token.ServerId + " is " +
                                         serverCollateralBalance.ToString() + ", original registration height " +
                                         registrationRecord.BlockReceived + ", current height " + height);

                    if (serverCollateralBalance.ToUnit(MoneyUnit.BTC) < this.network.Consensus.ServiceNodeCollateralThreshold &&
                        height - registrationRecord.BlockReceived > this.network.Consensus.ServiceNodeCollateralBlockPeriod)
                    {
                        // Remove server registrations as funding has not been performed within block count,
                        // or funds have been removed from the collateral address subsequent to the
                        // registration being performed
                        this.logger.LogDebug("Insufficient collateral within window period for server: " + registrationRecord.Token.ServerId);
                        this.logger.LogDebug("Deleting registration records for server: " + registrationRecord.Token.ServerId);
                        this.registrationStore.DeleteAllForServer(registrationRecord.Token.ServerId);
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
