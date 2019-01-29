using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using Newtonsoft.Json.Linq;
using Redstone.Features.ServiceNode.Common;
using Redstone.Features.ServiceNode.Models;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Features.Wallet.Models;

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

        private readonly IBroadcasterManager broadcasterManager;

        private readonly string regStorePath;

        public ServiceNodeRegistration(Network network, NodeSettings nodeSettings, IWalletManager walletManager, IBroadcasterManager broadcasterManager)
        {
            this.network = network;
            this.walletManager = walletManager;
            this.broadcasterManager = broadcasterManager;
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

        public async Task<Transaction> PerformRegistrationAsync(IServiceNodeRegistrationConfig registrationConfig, string walletName, string accountName, BitcoinSecret privateKeyEcdsa, RsaKey serviceRsaKey)
        {
            RegistrationToken registrationToken = new RegistrationToken(this.PROTOCOL_VERSION_TO_USE, registrationConfig.ServiceEcdsaKeyAddress, registrationConfig.Ipv4Address, registrationConfig.Ipv6Address, registrationConfig.OnionAddress, registrationConfig.ConfigurationHash, registrationConfig.Port, privateKeyEcdsa.PubKey);
            byte[] msgBytes = registrationToken.GetRegistrationTokenBytes(serviceRsaKey, privateKeyEcdsa);

            // Create the registration transaction using the bytes generated above
            Transaction rawTx = CreateBreezeRegistrationTx(network, msgBytes, registrationConfig.TxOutputValue);

            RegistrationStore regStore = new RegistrationStore(regStorePath);

            try
            {
                IEnumerable<UnspentOutputReference> spendableTransactions = this.walletManager.GetSpendableTransactionsInAccount(new WalletAccountReference(walletName, accountName));

                Transaction fundedTx = TransactionUtils.FundTransaction(spendableTransactions,
                    rawTx,
                    registrationConfig.TxFeeValue,
                    BitcoinAddress.Create(registrationConfig.ServiceEcdsaKeyAddress));

                fundedTx.Sign(this.network, privateKeyEcdsa, false);

                regStore.Add(new RegistrationRecord(
                    DateTime.Now,
                    Guid.NewGuid(),
                    fundedTx.GetHash().ToString(),
                    fundedTx.ToHex(),
                    registrationToken,
                    null));

                await this.broadcasterManager.BroadcastTransactionAsync(fundedTx).ConfigureAwait(false);

                return fundedTx;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Unable to broadcast registration transaction");
                Console.WriteLine(e);
            }

            return null;
        }



        /// <remarks>
        /// Funding of the transaction is handled by the 'fundrawtransaction' RPC
        /// call or its equivalent reimplementation.
        /// Only construct the transaction outputs; the change address is handled
        /// automatically by the funding logic
        /// You need to control *where* the change address output appears inside the
        /// transaction to prevent decoding errors with the addresses. Note that if
        /// the fundrawtransaction RPC call is used there is an option that can be
        /// passed to specify the position of the change output (it is randomly
        /// positioned otherwise)
        /// </remarks>
        public Transaction CreateBreezeRegistrationTx(Network network, byte[] data, Money outputValue)
        {
            Transaction sendTx = new Transaction();

            byte[] bytes = Encoding.UTF8.GetBytes(RegistrationToken.Marker);
            sendTx.Outputs.Add(new TxOut()
            {
                Value = outputValue,
                ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(bytes)
            });

            // Add each data-encoding PubKey as a TxOut
            foreach (PubKey pubKey in BlockChainDataConversions.BytesToPubKeys(data))
            {
                TxOut destTxOut = new TxOut()
                {
                    Value = outputValue,
                    ScriptPubKey = pubKey.ScriptPubKey
                };

                sendTx.Outputs.Add(destTxOut);
            }

            if (sendTx.Outputs.Count == 0)
                throw new Exception("ERROR: No outputs in registration transaction, cannot proceed");

            return sendTx;
        }
    }
}
