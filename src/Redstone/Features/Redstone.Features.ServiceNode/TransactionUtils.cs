using System;
using System.Collections.Generic;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.Wallet;

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

        public static Transaction FundTransaction(IEnumerable<UnspentOutputReference> spendableTransactions, Transaction rawTx, Money feeAmount, BitcoinAddress changeAddress)
        {
            // The transaction funding logic will ensure that a transaction fee of
            // feeAmount is included. The remaining difference between the value of
            // the inputs and the outputs will be returned as a change address output
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

            return rawTx;
        }
    }
}