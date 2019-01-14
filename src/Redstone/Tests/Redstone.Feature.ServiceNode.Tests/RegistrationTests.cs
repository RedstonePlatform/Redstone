using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using Redstone.Core.Networks;
using Redstone.Features.ServiceNode;
using Redstone.Features.ServiceNode.Common;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.IntegrationTests.Common;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Xunit;

namespace Redstone.Feature.ServiceNode.Tests
{
    public class Tests
    {
        private const string Password = "RegistrationWallet1Password";
        private const string WalletName = "RegistrationWallet1";
        private const string Passphrase = "RegistrationWallet1Passphrase";
        private const string Account = "account 0";
        private readonly Network network = RedstoneNetworks.RegTest;

        [Fact]
        public void RegistrationTransactionCanBeParsed()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                CoreNode node1 = builder.CreateRedstonePosNode(network).WithWallet(Password, WalletName, Passphrase).Start();

                var transaction = CreateRegistrationTransaction(node1);

                var registrationToken = new RegistrationToken();
                registrationToken.ParseTransaction(transaction, network);
            }
        }

        [Fact]
        public void RegistrationTest()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                CoreNode node1 = builder.CreateRedstonePosNode(network).WithWallet(Password, WalletName, Passphrase).Start();
                CoreNode node2 = builder.CreateRedstonePosNode(network).Start();

                int maturity = (int)network.Consensus.CoinbaseMaturity;
                TestHelper.MineBlocks(node1, maturity + 5, true, WalletName, Password);

                CreateTransactionsAndBroadcast(node1, 4);
                TestHelper.MineBlocks(node1, 1, true, WalletName, Password);

                CreateRTAndB(node1, 5);
                TestHelper.MineBlocks(node1, 1, true, WalletName, Password);
                TestHelper.ConnectAndSync(node1, node2);

                var rm2 = node2.FullNode.NodeService<RegistrationManager>();
                var rs2 = rm2.GetRegistrationStore();

                foreach (var record in rs2.GetAll())
                {
                    Console.WriteLine("Received registration: " + record.RecordTxId);
                }
            }
        }

        private static void CreateTransactionsAndBroadcast(CoreNode node1, int transactionCount)
        {
            var trxs = new List<Transaction>();
            foreach (int index in Enumerable.Range(1, transactionCount))
            {
                Block block = node1.FullNode.BlockStore().GetBlockAsync(node1.FullNode.Chain.GetBlock(index).HashBlock).Result;
                Transaction prevTrx = block.Transactions.First();
                var dest = new BitcoinSecret(new Key(), node1.FullNode.Network);

                Transaction tx = node1.FullNode.Network.CreateTransaction();
                tx.AddInput(new TxIn(new OutPoint(prevTrx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(node1.MinerSecret.PubKey)));
                tx.AddOutput(new TxOut("5", dest.PubKey.Hash));
                tx.AddOutput(new TxOut("3", new Key().PubKey.Hash)); // 1 btc fee
                tx.Sign(node1.FullNode.Network, node1.MinerSecret, false);
                trxs.Add(tx);
            }
            var options = new ParallelOptions { MaxDegreeOfParallelism = transactionCount };
            Parallel.ForEach(trxs, options, transaction =>
            {
                node1.Broadcast(transaction);
            });
        }

        private static void CreateRTAndB(CoreNode node1, int index)
        {
            Block block = node1.FullNode.BlockStore().GetBlockAsync(node1.FullNode.Chain.GetBlock(index).HashBlock).Result;
            Transaction prevTrx = block.Transactions.First();

            var transaction = CreateRegistrationTransaction(node1);
            transaction.AddInput(new TxIn(new OutPoint(prevTrx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(node1.MinerSecret.PubKey)));
            transaction.Sign(node1.FullNode.Network, node1.MinerSecret, false);
            node1.Broadcast(transaction);
        }

        private static Transaction CreateRegistrationTransaction(CoreNode node1)
        {
            var outputAmount = new Money(0.0123m, MoneyUnit.BTC);

            var redstonemarker = Encoding.UTF8.GetBytes("REDSTONE_SN_REGISTRATION_MARKER");
            var scriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(redstonemarker);

            Transaction tx = node1.FullNode.Network.CreateTransaction();
            tx.AddOutput(new TxOut(outputAmount, scriptPubKey.Hash));

            var rsa = new RsaKey();
            var ecdsa = new BitcoinSecret(new Key(), node1.FullNode.Network);
            var serverAddress = ecdsa.GetAddress().ToString();

            var token = new RegistrationToken(1,
                serverAddress,
                IPAddress.Parse("127.0.0.1"),
                IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
                "0123456789ABCDEF",
                "0123456789012345678901234567890123456789",
                37123,
                ecdsa.PubKey);

            var cryptoUtils = new CryptoUtils(rsa, ecdsa);
            token.RsaSignature = cryptoUtils.SignDataRSA(token.GetHeaderBytes().ToArray());
            token.EcdsaSignature = cryptoUtils.SignDataECDSA(token.GetHeaderBytes().ToArray());

            byte[] msgBytes = token.GetRegistrationTokenBytes(rsa, ecdsa);

            foreach (PubKey pubKey in BlockChainDataConversions.BytesToPubKeys(msgBytes))
            {
                tx.AddOutput(new TxOut(outputAmount, pubKey.ScriptPubKey));
            }

            return tx;
        }
    }
}
