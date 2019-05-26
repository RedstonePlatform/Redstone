using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NBitcoin;
using Redstone.Features.ServiceNode.Common;
using Redstone.Features.ServiceNode.Models;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Wallet.Interfaces;

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

        private readonly IWalletTransactionHandler walletTransactionHandler;

        private readonly IWalletManager walletManager;

        private readonly IBroadcasterManager broadcasterManager;

        private readonly string regStorePath;

        public ServiceNodeRegistration(Network network, 
            NodeSettings nodeSettings,
            IBroadcasterManager broadcasterManager, 
            IWalletTransactionHandler walletTransactionHandler,
            IWalletManager walletManager)
        {
            this.network = network;
            this.broadcasterManager = broadcasterManager;
            this.walletTransactionHandler = walletTransactionHandler;
            this.walletManager = walletManager;
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

            var ecsdaPubKeyAddress = registrationConfig.EcdsaPrivateKey.GetAddress().ToString();
            List<RegistrationRecord> transactions = regStore.GetByServerId(ecsdaPubKeyAddress);

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
                registrationToken = mostRecent.Token;
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

        public async Task<Transaction> PerformRegistrationAsync(IServiceNodeRegistrationConfig registrationConfig,
            string walletName, string walletPassword, string accountName, RsaKey serviceRsaKey)
        {
            Transaction transaction = null;
            try
            {
                RegistrationToken registrationToken = registrationConfig.CreateRegistrationToken(this.network);

                transaction = TransactionUtils.BuildTransaction(this.network, 
                    this.walletTransactionHandler, 
                    this.walletManager,
                    registrationConfig, 
                    registrationToken, 
                    walletName, 
                    accountName, 
                    walletPassword, 
                    serviceRsaKey);

                await this.broadcasterManager.BroadcastTransactionAsync(transaction).ConfigureAwait(false);

                var regStore = new RegistrationStore(this.regStorePath);
                regStore.Add(new RegistrationRecord(
                    DateTime.Now,
                    Guid.NewGuid(),
                    transaction.GetHash().ToString(),
                    transaction.ToHex(),
                    registrationToken,
                    null));
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Unable to broadcast registration transaction");
                Console.WriteLine(e);
            }

            return transaction;
        }
    }
}
