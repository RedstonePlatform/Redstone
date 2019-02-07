using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using Redstone.Features.ServiceNode.Common;
using Redstone.Features.ServiceNode.Models;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;

namespace Redstone.Features.ServiceNode
{
    public static class TransactionUtils2
    {
        public static async Task<Transaction> PerformRegistrationAsync(Network network, IServiceNodeRegistrationConfig registrationConfig, IWalletTransactionHandler walletTransactionHandler, IWalletManager walletManager, IBroadcasterManager broadcasterManager, string walletName, string accountName, string password, string regStorePath, BitcoinSecret privateKeyEcdsa, RsaKey serviceRsaKey)
        {
            try
            {
                var registrationToken = registrationConfig.CreateRegistrationToken();

                Transaction transaction = BuildTransaction(network, walletTransactionHandler, registrationConfig, registrationToken, walletName, accountName, password, serviceRsaKey, privateKeyEcdsa);
                await broadcasterManager.BroadcastTransactionAsync(transaction).ConfigureAwait(false);

                var regStore = new RegistrationStore(regStorePath);
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

        public static Transaction BuildTransaction(
            Network network, 
            IWalletTransactionHandler walletTransactionHandler, 
            IServiceNodeRegistrationConfig registrationConfig, 
            RegistrationToken registrationToken, 
            string walletName, 
            string accountName, 
            string password, 
            RsaKey serviceRsaKey, 
            BitcoinSecret privateKeyEcdsa)
        {
            var accountReference = new WalletAccountReference()
            {
                AccountName = accountName,
                WalletName = walletName
            };

            var context = new TransactionBuildContext(network)
            {
                AccountReference = accountReference,
                Recipients = GetRecipients(registrationConfig, registrationToken, serviceRsaKey, privateKeyEcdsa),
                Shuffle = false,
                Sign = true,
                OverrideFeeRate = new FeeRate(registrationConfig.TxFeeValue),
                WalletPassword = password,
            };
            context.TransactionBuilder.CoinSelector = new DefaultCoinSelector
            {
                GroupByScriptPubKey = false
            };
            Transaction transaction = walletTransactionHandler.BuildTransaction(context);

            return transaction;
        }

        private static List<Recipient> GetRecipients(IServiceNodeRegistrationConfig registrationConfig, RegistrationToken registrationToken, RsaKey serviceRsaKey, BitcoinSecret privateKeyEcdsa)
        {
            byte[] tokenBytes = registrationToken.GetRegistrationTokenBytes(serviceRsaKey, privateKeyEcdsa);
            byte[] markerBytes = Encoding.UTF8.GetBytes(RegistrationToken.Marker);

            var recipients = new List<Recipient>
            {
                new Recipient
                {
                    Amount = registrationConfig.TxOutputValue,
                    ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(markerBytes)
                }
            };

            recipients.AddRange(BlockChainDataConversions.BytesToPubKeys(tokenBytes).Select(pk => new Recipient
            {
                Amount = registrationConfig.TxOutputValue,
                ScriptPubKey = pk.ScriptPubKey
            }));

            if (!recipients.Any())
                throw new Exception("ERROR: No recipients for registration transaction, cannot proceed");

            return recipients;
        }
    }
}