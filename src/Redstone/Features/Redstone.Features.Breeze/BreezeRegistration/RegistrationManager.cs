﻿using System;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.Features.Breeze.BreezeCommon;
using Stratis.Bitcoin.Features.WatchOnlyWallet;

namespace Redstone.Features.Breeze.BreezeRegistration
{
    public class RegistrationManager : IRegistrationManager
    {
        public static readonly Money MASTERNODE_COLLATERAL_THRESHOLD = new Money(5, MoneyUnit.BTC);
        public static readonly int MAX_PROTOCOL_VERSION = 128; // >128 = regard as test versions
        public static readonly int MIN_PROTOCOL_VERSION = 1;
        public static readonly int WINDOW_PERIOD_BLOCK_COUNT = 30;

        private ILoggerFactory loggerFactory;
        private RegistrationStore registrationStore;
        private Network network;
        private WatchOnlyWalletManager watchOnlyWalletManager;

        private ILogger logger;

        public void Initialize(ILoggerFactory loggerFactory, RegistrationStore registrationStore, bool isBitcoin, Network network, IWatchOnlyWalletManager watchOnlyWalletManager)
        {
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.registrationStore = registrationStore;
            this.network = network;
            this.watchOnlyWalletManager = watchOnlyWalletManager as WatchOnlyWalletManager;

            this.logger.LogInformation("Initialized RegistrationFeature");
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
                    // Minor optimisation to disregard transactions that cannot be registrations
                    if (tx.Outputs.Count < 2)
                        continue;

                    // Check if the transaction has the Breeze registration marker output (literal text BREEZE_REGISTRATION_MARKER)
                    if (!tx.Outputs[0].ScriptPubKey.ToHex().ToLower()
                        .Equals("6a1a425245455a455f524547495354524154494f4e5f4d41524b4552")) continue;

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

                        if ((serverCollateralBalance < MASTERNODE_COLLATERAL_THRESHOLD) &&
                            ((height - record.BlockReceived) > WINDOW_PERIOD_BLOCK_COUNT))
                        {
                            // Remove server registrations as funding has not been performed timeously,
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

        public void Dispose()
        {
        }
    }
}
