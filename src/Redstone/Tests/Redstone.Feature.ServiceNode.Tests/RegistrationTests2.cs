using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NBitcoin;
using Redstone.Core.Networks;
using Redstone.Features.ServiceNode;
using Redstone.Features.ServiceNode.Common;
using Redstone.Features.ServiceNode.Models;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.IntegrationTests.Common;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Xunit;

namespace Redstone.Feature.ServiceNode.Tests
{
    public class Tests2
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

                CreateRegistrationTransactionAndBroadcast(node1, 5);
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

        private void CreateRegistrationTransactionAndBroadcast(CoreNode node1, int index)
        {
            Transaction transaction = CreateRegistrationTransaction(node1);

            node1.Broadcast(transaction);
        }

        private Transaction CreateRegistrationTransaction(CoreNode node1)
        {
            var rsa = new RsaKey();
            var ecdsa = new BitcoinSecret(new Key(), node1.FullNode.Network);
            var serverAddress = ecdsa.GetAddress().ToString();

            var config = new ServiceNodeRegistrationConfig
            {
                ProtocolVersion = (int)ServiceNodeProtocolVersion.INITIAL,
                ServerId = serverAddress,
                Ipv4Address = IPAddress.Parse("127.0.0.1"),
                Port = 37123,
                ConfigurationHash = "0123456789012345678901234567890123456789", // TODO hash of config file
                ServiceEcdsaKeyAddress = ecdsa.GetAddress().ToString(),
                EcdsaPubKey = ecdsa.PubKey,
            };

            (RegistrationToken token, Transaction transaction) = TransactionUtils.CreateRegistrationTransaction(
                this.network, 
                config, 
                rsa, 
                ecdsa);

            IWalletManager walletManager = node1.FullNode.NodeService<IWalletManager>();
            TransactionUtils.FundTransaction(
                walletManager,
                WalletName,
                Account,
                transaction,
                config.TxFeeValue,
                BitcoinAddress.Create(config.ServiceEcdsaKeyAddress));

            //TransactionUtils.SignTransaction(
            //    transaction,
            //    walletManager,
            //    this.network,
            //    WalletName,
            //    Password,
            //    Account);

            //transaction.Sign(node1.FullNode.Network, ecdsa, false);
            transaction.Sign(node1.FullNode.Network, node1.MinerSecret, false);

            return transaction;
        }
    }
}
