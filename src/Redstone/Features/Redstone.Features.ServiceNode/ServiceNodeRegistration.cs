﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using Redstone.Features.ServiceNode.Common;
using Redstone.Features.ServiceNode.Models;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Broadcasting;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Utilities.JsonErrors;

namespace Redstone.Features.ServiceNode
{
    public enum ServiceNodeProtocolVersion
    {
        INITIAL = 1,
        TESTNET_INITIAL = 253,
    }

    public class ServiceNodeRegistration
    {
        private readonly Network network;

        private readonly IWalletManager walletManager;

        private readonly IWalletTransactionHandler walletTransactionHandler;

        private readonly IBroadcasterManager broadcasterManager;

        private readonly string regStorePath;

        public ServiceNodeRegistration(Network network, NodeSettings nodeSettings, IWalletManager walletManager, IBroadcasterManager broadcasterManager, IWalletTransactionHandler walletTransactionHandler)
        {
            this.network = network;
            this.walletManager = walletManager;
            this.broadcasterManager = broadcasterManager;
            this.walletTransactionHandler = walletTransactionHandler;
            this.regStorePath = Path.Combine(nodeSettings.DataDir, "registrationHistory.json");
        }

        // 254 = potentially nonsensical data from internal tests. 253 will be the public testnet version
        // 1 = mainnet protocol version incorporating signature check
        private int PROTOCOL_VERSION_TO_USE = (int)ServiceNodeProtocolVersion.INITIAL;

        public bool IsRegistrationValid(IServiceNodeRegistrationConfig registrationConfig)
        {
            // In order to determine if the registration sequence has been performed
            // before, and to see if a previous performance is still valid, interrogate
            // the database to see if any transactions have been recorded.

            RegistrationStore regStore = new RegistrationStore(regStorePath);

            List<RegistrationRecord> transactions = regStore.GetByServerId(registrationConfig.ServiceEcdsaKeyAddress);

            // If no transactions exist, the registration definitely needs to be done
            if (transactions == null || transactions.Count == 0)
            {
                return false;
            }

            RegistrationRecord mostRecent = null;
            foreach (RegistrationRecord record in transactions)
            {
                // Find most recent transaction
                if (mostRecent == null)
                {
                    mostRecent = record;
                }

                if (record.RecordTimestamp > mostRecent.RecordTimestamp)
                    mostRecent = record;
            }

            // Check if the stored record matches the current configuration
            RegistrationToken registrationToken;
            try
            {
                registrationToken = mostRecent.Record;
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine(e);
                return false;
            }

            // IPv4
            if (registrationConfig.Ipv4Address == null && registrationToken.Ipv4Addr != null)
                return false;

            if (registrationConfig.Ipv4Address != null && registrationToken.Ipv4Addr == null)
                return false;

            if (registrationConfig.Ipv4Address != null
                && registrationToken.Ipv4Addr != null
                && !registrationConfig.Ipv4Address.Equals(registrationToken.Ipv4Addr))
                return false;

            // IPv6
            if (registrationConfig.Ipv6Address == null && registrationToken.Ipv6Addr != null)
                return false;

            if (registrationConfig.Ipv6Address != null && registrationToken.Ipv6Addr == null)
                return false;

            if (registrationConfig.Ipv6Address != null
                && registrationToken.Ipv6Addr != null
                && !registrationConfig.Ipv6Address.Equals(registrationToken.Ipv6Addr))
                return false;

            // Onion
            if (registrationConfig.OnionAddress != registrationToken.OnionAddress)
                return false;

            if (registrationConfig.Port != registrationToken.Port)
                return false;

            // This verifies that the parameters are unchanged
            if (registrationConfig.ConfigurationHash != registrationToken.ConfigurationHash)
                return false;

            // TODO: Check if transaction is actually confirmed on the blockchain?

            return true;
        }

        public async Task<Transaction> PerformRegistrationNewAsync(IServiceNodeRegistrationConfig registrationConfig,
            string walletName, string walletPassword, string accountName, BitcoinSecret privateKeyEcdsa,
            RsaKey serviceRsaKey)
        {
            var tx = await TransactionUtils2.PerformRegistrationAsync(this.network,
                registrationConfig,
                this.walletTransactionHandler,
                this.walletManager,
                this.broadcasterManager,
                walletName,
                accountName,
                walletPassword,
                this.regStorePath,
                privateKeyEcdsa,
                serviceRsaKey).ConfigureAwait(false);

            return tx;
        }

        public async Task<Transaction> PerformRegistrationAsync(IServiceNodeRegistrationConfig registrationConfig, string walletName, string walletPassword, string accountName, BitcoinSecret privateKeyEcdsa, RsaKey serviceRsaKey)
        {
            try
            {   
                (RegistrationToken registrationToken, Transaction transaction) = TransactionUtils.CreateRegistrationTransaction(
                    this.network,
                    registrationConfig,
                    serviceRsaKey,
                    privateKeyEcdsa);

                TransactionUtils.FundTransaction(this.walletManager,
                    walletName, 
                    accountName,
                    transaction,
                    registrationConfig.TxFeeValue,
                    BitcoinAddress.Create(registrationConfig.ServiceEcdsaKeyAddress));

                TransactionUtils.SignTransaction(transaction,
                    this.walletManager,
                    this.network, 
                    walletName, 
                    walletPassword, 
                    accountName);
                await this.broadcasterManager.BroadcastTransactionAsync(transaction).ConfigureAwait(false);

                var regStore = new RegistrationStore(this.regStorePath);
                regStore.Add(new RegistrationRecord(
                    DateTime.Now,
                    Guid.NewGuid(),
                    transaction.GetHash().ToString(),
                    transaction.ToHex(),
                    registrationToken,
                    null));

                return transaction;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Unable to broadcast registration transaction");
                Console.WriteLine(e);
            }

            return null;
        }
    }
}
