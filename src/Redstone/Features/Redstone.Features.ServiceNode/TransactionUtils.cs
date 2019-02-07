using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Redstone.Features.ServiceNode.Common;
using Redstone.Features.ServiceNode.Models;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;

namespace Redstone.Features.ServiceNode
{
    public static class TransactionUtils
    {
        public static Transaction LoadTransactionFromHex(string hex, Network network, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
        {
            if (hex == null)
                throw new ArgumentNullException(nameof(hex));

            if (network == null)
                throw new ArgumentNullException(nameof(network));

            Transaction transaction = network.Consensus.ConsensusFactory.CreateTransaction();
            transaction.FromBytes(Encoders.Hex.DecodeData(hex), network.Consensus.ConsensusFactory, version);
            return transaction;
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
        public static (RegistrationToken, Transaction) CreateRegistrationTransaction(
            Network network,
            IServiceNodeRegistrationConfig registrationConfig,
            RsaKey serviceRsaKey,
            BitcoinSecret privateKeyEcdsa)
        {
            RegistrationToken registrationToken = registrationConfig.CreateRegistrationToken();
            byte[] msgBytes = registrationToken.GetRegistrationTokenBytes(serviceRsaKey, privateKeyEcdsa);

            var cryptoUtils = new CryptoUtils(serviceRsaKey, privateKeyEcdsa);
            registrationToken.RsaSignature = cryptoUtils.SignDataRSA(registrationToken.GetHeaderBytes().ToArray());
            registrationToken.EcdsaSignature = cryptoUtils.SignDataECDSA(registrationToken.GetHeaderBytes().ToArray());

            Script scriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(Encoding.UTF8.GetBytes(RegistrationToken.Marker));

            Transaction transaction = network.CreateTransaction();
            transaction.AddOutput(new TxOut(registrationConfig.TxOutputValue, scriptPubKey.Hash));

            foreach (PubKey pubKey in BlockChainDataConversions.BytesToPubKeys(msgBytes))
            {
                transaction.AddOutput(new TxOut(registrationConfig.TxOutputValue, pubKey.ScriptPubKey));
            }

            return (registrationToken, transaction);

            //Transaction sendTx = new Transaction();

            //byte[] bytes = Encoding.UTF8.GetBytes(RegistrationToken.Marker);
            //sendTx.Outputs.Add(new TxOut()
            //{
            //    Value = outputValue,
            //    ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(bytes)
            //});

            //// Add each data-encoding PubKey as a TxOut
            //foreach (PubKey pubKey in BlockChainDataConversions.BytesToPubKeys(data))
            //{
            //    TxOut destTxOut = new TxOut()
            //    {
            //        Value = outputValue,
            //        ScriptPubKey = pubKey.ScriptPubKey
            //    };

            //    sendTx.Outputs.Add(destTxOut);
            //}

            //if (sendTx.Outputs.Count == 0)
            //    throw new Exception("ERROR: No outputs in registration transaction, cannot proceed");

            //return sendTx;
        }

        // The transaction funding logic will ensure that a transaction fee of
        // feeAmount is included. The remaining difference between the value of
        // the inputs and the outputs will be returned as a change address output
        public static void FundTransaction(IWalletManager walletManager, string walletName, string accountName, Transaction rawTx, Money feeAmount, BitcoinAddress changeAddress)
        {
            IEnumerable<UnspentOutputReference> spendableTransactions = walletManager.GetSpendableTransactionsInAccount(new WalletAccountReference(walletName, accountName));

            var totalFunded = new Money(0);

            foreach (var spendable in spendableTransactions)
            {
                if (totalFunded < (rawTx.TotalOut + feeAmount))
                {
                    rawTx.Inputs.Add(new TxIn()
                    {
                        PrevOut = spendable.ToOutPoint()
                    });

                    // By this point the input array will have at least one element
                    // starting at index 0
                    rawTx.Inputs[rawTx.Inputs.Count - 1].ScriptSig = spendable.Transaction.ScriptPubKey;

                    // Need to accurately account for how much funding is assigned
                    // to the inputs so that change can be correctly calculated later
                    totalFunded += spendable.Transaction.Amount;
                }
                else
                {
                    break;
                }
            }

            if (totalFunded < (rawTx.TotalOut + feeAmount))
                throw new Exception("Insufficient unspent funds for registration");

            var change = totalFunded - rawTx.TotalOut - feeAmount;

            if (change < 0)
                throw new Exception("Change amount cannot be negative for registration transaction");

            rawTx.Outputs.Add(new TxOut()
            {
                Value = change,
                ScriptPubKey = changeAddress.ScriptPubKey
            });
        }

        public static void SignTransaction(Transaction transaction, IWalletManager walletManager, Network network, string walletName, string walletPassword, string accountName)
        {
            HdAddress unusedAddress = walletManager.GetUnusedAddress(new WalletAccountReference(walletName, accountName));
            Wallet wallet = walletManager.GetWalletByName(walletName);
            Key extendedPrivateKey = wallet.GetExtendedPrivateKeyForAddress(walletPassword, unusedAddress).PrivateKey;
            var signingKey = new BitcoinSecret(extendedPrivateKey, network);
            transaction.Sign(network, signingKey, false);
        }
    }
}