using System;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.Features.ServiceNode.Common;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.WatchOnlyWallet;
using Stratis.Bitcoin.Primitives;
using Stratis.Bitcoin.Signals;

namespace Redstone.Features.ServiceNode
{
    public class RegistrationManager : IRegistrationManager
    {
        public static readonly Money SERVICENODE_COLLATERAL_THRESHOLD = new Money(1500, MoneyUnit.BTC);
        public static readonly int MAX_PROTOCOL_VERSION = 128; // >128 = regard as test versions
        public static readonly int MIN_PROTOCOL_VERSION = 1;
        public static readonly int WINDOW_PERIOD_BLOCK_COUNT = 30;

        private RegistrationStore registrationStore;
        private Network network;
        private WatchOnlyWalletManager watchOnlyWalletManager;
        private ISignals signals;
        private ILogger logger;

        public RegistrationManager(
            ILoggerFactory loggerFactory,
            NodeSettings nodeSettings,
            RegistrationStore registrationStore,
            ISignals signals,
            IWatchOnlyWalletManager watchOnlyWalletManager)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.registrationStore = registrationStore;
            this.network = nodeSettings.Network;
            this.signals = signals;
            this.watchOnlyWalletManager = watchOnlyWalletManager as WatchOnlyWalletManager;
            this.logger.LogInformation("Initialized RegistrationFeature");
        }

        public void Initialize()
        {
            this.watchOnlyWalletManager.Initialize();
            this.signals.OnBlockConnected.Attach(this.OnBlockConnected);
        }

        public void Dispose()
        {
            this.signals.OnBlockConnected.Detach(this.OnBlockConnected);
        }

        private void OnBlockConnected(ChainedHeaderBlock chBlock)
        {
            this.ProcessBlock(chBlock.ChainedHeader.Height, chBlock.Block); this.watchOnlyWalletManager.ProcessBlock(chBlock.Block);
        }

        public RegistrationStore GetRegistrationStore()
        {
            return this.registrationStore;
        }

        /// <inheritdoc />
        public void ProcessBlock(int height, Block block)
        {
            this.logger.LogTrace("()");

            // Check for any server registration transactions
            if (block.Transactions != null)
            {
                foreach (Transaction tx in block.Transactions)
                {
                    if (!RegistrationToken.HasMarker(tx))
                        continue;

                    this.logger.LogDebug("Received a new registration transaction: " + tx.GetHash());

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
                        if ((registrationRecord.Record.ProtocolVersion < MIN_PROTOCOL_VERSION) ||
                            (registrationRecord.Record.ProtocolVersion > MAX_PROTOCOL_VERSION))
                        {
                            this.logger.LogDebug("Registration protocol version out of bounds " + tx.GetHash());
                            continue;
                        }

                        // If there were other registrations for this server previously, remove them and add the new one                         
                        this.logger.LogTrace("Registrations - AddWithReplace");

                        this.registrationStore.AddWithReplace(registrationRecord);

                        this.logger.LogTrace("Registration transaction for server collateral address: " + registrationRecord.Record.ServerId);
                        this.logger.LogTrace("Server Onion address: " + registrationRecord.Record.OnionAddress);
                        this.logger.LogTrace("Server configuration hash: " + registrationRecord.Record.ConfigurationHash);

                        // Add collateral address to watch only wallet so that any funding transactions can be detected
                        this.watchOnlyWalletManager.WatchAddress(registrationRecord.Record.ServerId);
                    }
                    catch (Exception e)
                    {
                        this.logger.LogDebug("Failed to parse registration transaction, exception: " + e);
                    }
                }

                WatchOnlyWallet watchOnlyWallet = this.watchOnlyWalletManager.GetWatchOnlyWallet();

                // TODO: Need to have 'current height' field in watch-only wallet so that we don't start rebalancing collateral balances before the latest block has been processed & incorporated

                // Perform watch-only wallet housekeeping - iterate through known servers
                foreach (RegistrationRecord record in this.registrationStore.GetAll())
                {
                    try
                    {
                        Script scriptToCheck = BitcoinAddress.Create(record.Record.ServerId, this.network).ScriptPubKey;

                        this.logger.LogDebug("Recalculating collateral balance for server: " + record.Record.ServerId);

                        if (!watchOnlyWallet.WatchedAddresses.ContainsKey(scriptToCheck.ToString()))
                        {
                            this.logger.LogDebug(
                                "Server address missing from watch-only wallet. Deleting stored registrations for server: " +
                                record.Record.ServerId);
                            this.registrationStore.DeleteAllForServer(record.Record.ServerId);
                            continue;
                        }

                        Money serverCollateralBalance =
                            this.watchOnlyWalletManager.GetRelativeBalance(record.Record.ServerId);

                        this.logger.LogDebug("Collateral balance for server " + record.Record.ServerId + " is " +
                                             serverCollateralBalance.ToString() + ", original registration height " +
                                             record.BlockReceived + ", current height " + height);

                        if ((serverCollateralBalance < SERVICENODE_COLLATERAL_THRESHOLD) &&
                            ((height - record.BlockReceived) > WINDOW_PERIOD_BLOCK_COUNT))
                        {
                            // Remove server registrations as funding has not been performed within block count,
                            // or funds have been removed from the collateral address subsequent to the
                            // registration being performed
                            this.logger.LogDebug("Insufficient collateral within window period for server: " + record.Record.ServerId);
                            this.logger.LogDebug("Deleting registration records for server: " + record.Record.ServerId);
                            this.registrationStore.DeleteAllForServer(record.Record.ServerId);

                            // TODO: Remove unneeded transactions from the watch-only wallet?
                            // TODO: Need to make the TumbleBitFeature change its server address if this is the address it was using
                        }
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError("Error calculating server collateral balance: " + e);
                    }
                }

                this.logger.LogTrace("SaveWatchOnlyWallet");

                this.watchOnlyWalletManager.SaveWatchOnlyWallet();
            }

            this.logger.LogTrace("(-)");
        }
    }
}
