﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBitcoin;
using Redstone.ServiceNode.Models;
using Redstone.ServiceNode.Utils;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;

namespace Redstone.Features.ServiceNode
{
    public static class TransactionUtils
    {
        public static Transaction BuildTransaction(
            Network network,
            IWalletTransactionHandler walletTransactionHandler,
            IServiceNodeRegistrationConfig registrationConfig,
            RegistrationToken registrationToken,
            string walletName,
            string accountName,
            string password)
        {
            var accountReference = new WalletAccountReference()
            {
                AccountName = accountName,
                WalletName = walletName
            };
            
            var context = new TransactionBuildContext(network)
            {
                AccountReference = accountReference,
                Recipients = GetRecipients(registrationConfig, registrationToken),
                Shuffle = false,
                Sign = true,
                OverrideFeeRate = new FeeRate(registrationConfig.TxFeeValue),
                WalletPassword = password
            };
            context.TransactionBuilder.CoinSelector = new DefaultCoinSelector
            {
                GroupByScriptPubKey = false
            };
            Transaction transaction = walletTransactionHandler.BuildTransaction(context);

            return transaction;
        }

        private static List<Recipient> GetRecipients(IServiceNodeRegistrationConfig registrationConfig, RegistrationToken registrationToken)
        {
            byte[] tokenBytes = registrationToken.GetRegistrationTokenBytes(registrationConfig.PrivateKey);
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