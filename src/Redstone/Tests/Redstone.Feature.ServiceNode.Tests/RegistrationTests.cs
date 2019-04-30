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
using Stratis.Bitcoin.Features.WatchOnlyWallet;
using Stratis.Bitcoin.IntegrationTests.Common;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Redstone.Feature.ServiceNode.Tests
{
    public class RegistrationTests
    {
        private readonly ITestOutputHelper output;
        private const string Password = "RegistrationWallet1Password";
        private const string WalletName = "RegistrationWallet1";
        private const string Passphrase = "RegistrationWallet1Passphrase";
        private const string Account = "account 0";
        private readonly Network network = RedstoneNetworks.RegTest;
        private CoreNode node;
        private CoreNode watchingNode;
        private RegistrationManager rm;
        private RegistrationStore rs;
        private WatchOnlyWalletManager wowm;
        private RegistrationManager watchingRm;
        private RegistrationStore watchingRs;
        private WatchOnlyWalletManager watchingWowm;
        private BitcoinSecret serverSecret;
        private readonly Money collateral;
        private readonly int maturity;

        public RegistrationTests(ITestOutputHelper output)
        {
            this.collateral = new Money(100, MoneyUnit.BTC);
            this.maturity = (int)this.network.Consensus.CoinbaseMaturity;
            this.output = output;
        }

        [Fact]
        public void RegistrationTransactionCanBeParsed()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                Setup(builder);

                TestHelper.MineBlocks(this.node, this.maturity + 5, true, WalletName, Password);

                CreateTransactionsAndBroadcast(1);
                TestHelper.MineBlocks(this.node, 1, true, WalletName, Password);

                var serverSecret = new BitcoinSecret(new Key(), this.node.FullNode.Network);
                Transaction transaction = CreateRegistrationTransaction();

                var registrationToken = new RegistrationToken();
                registrationToken.ParseTransaction(transaction, this.network);
                Assert.True(registrationToken.Validate(this.network));
            }
        }

        [Fact]
        public void RegistrationTest()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                Setup(builder);

                // Seed 
                MineConnectAndSync(this.maturity + 5);
                CreateTransactionsAndBroadcast(4);
                MineConnectAndSync();
                AssertStoreAndBalance(0, null);

                // Register
                CreateRegistrationTransactionAndBroadcast();
                MineConnectAndSync();
                AssertStoreAndBalance(1, new Money(0, MoneyUnit.BTC));

                // Pay Collateral
                CreateTransactionAndBroadcast(this.collateral, this.serverSecret);
                ConnectAndSync();
                AssertStoreAndBalance(1, this.collateral);
                CreateTransactionAndBroadcast(this.collateral, this.serverSecret);
                ConnectAndSync();
                AssertStoreAndBalance(1, this.collateral);
                CreateTransactionAndBroadcast(this.collateral, this.serverSecret);
                ConnectAndSync();
                AssertStoreAndBalance(1, this.collateral);
                CreateTransactionAndBroadcast(this.collateral, this.serverSecret);
                ConnectAndSync();
                AssertStoreAndBalance(1, this.collateral);

                MineConnectAndSync();

                // Transactions up to collateral block period
                for (int i = 0; i < this.network.Consensus.ServiceNodeCollateralBlockPeriod; i++)
                {
                    CreateTransactionAndBroadcast("6");
                    MineConnectAndSync();
                    AssertStoreAndBalance(1, this.collateral);
                }
            }
        }

        private void Setup(NodeBuilder builder)
        {
            this.node = builder.CreateRedstonePosNode(this.network)
                                .WithWallet(Password, WalletName, Passphrase)
                                .Start();
            this.watchingNode = builder.CreateRedstonePosNode(this.network).Start();

            this.rm = this.node.FullNode.NodeService<RegistrationManager>();
            this.rs = this.rm.GetRegistrationStore();
            this.wowm = this.node.FullNode.NodeService<IWatchOnlyWalletManager>() as WatchOnlyWalletManager;

            this.watchingRm = this.watchingNode.FullNode.NodeService<RegistrationManager>();
            this.watchingRs = this.watchingRm.GetRegistrationStore();
            this.watchingWowm = this.watchingNode.FullNode.NodeService<IWatchOnlyWalletManager>() as WatchOnlyWalletManager;

            this.serverSecret = this.GetWalletPrivateKeyForServer();
        }

        private void MineConnectAndSync(int mineBlockCount = 1)
        {
            TestHelper.MineBlocks(this.node, mineBlockCount, true, WalletName, Password);
            ConnectAndSync();
        }

        private void ConnectAndSync(int mineBlockCount = 1)
        {
            TestHelper.ConnectAndSync(this.node, this.watchingNode);
        }

        private void AssertStoreAndBalance(int requiredStoreCount, Money requiredBalance)
        {
            Money balance = this.wowm.GetRelativeBalance(this.serverSecret.GetAddress().ToString());
            List<RegistrationRecord> records = this.rs.GetAll();

            Money watchingBalance = this.watchingWowm.GetRelativeBalance(this.serverSecret.GetAddress().ToString());
            List<RegistrationRecord> watchingRecords = this.watchingRs.GetAll();

            this.output.WriteLine($"Required {requiredBalance?.ToUnit(MoneyUnit.BTC)} Balance: {balance?.ToUnit(MoneyUnit.BTC)}");
            this.output.WriteLine($"Required {requiredBalance?.ToUnit(MoneyUnit.BTC)} WatchingBalance: {watchingBalance?.ToUnit(MoneyUnit.BTC)}");

            this.output.WriteLine($"Required {requiredStoreCount} StoreCount {records.Count}");
            this.output.WriteLine($"Required {requiredStoreCount} StoreCount {watchingRecords.Count}");
            //Assert.Equal(requiredBalance, balance);
            //Assert.Equal(requiredBalance, watchingBalance);
            //Assert.Equal(requiredStoreCount, records.Count);
            //Assert.Equal(requiredStoreCount, watchingRecords.Count);
        }

        private void CreateTransactionsAndBroadcast(int transactionCount)
        {
            var trxs = new List<Transaction>();
            foreach (int index in Enumerable.Range(1, transactionCount))
            {
                Block block = this.node.FullNode.BlockStore().GetBlock(this.node.FullNode.ChainIndexer.GetHeader(index).HashBlock);
                Transaction prevTrx = block.Transactions.First();
                var dest = new BitcoinSecret(new Key(), this.node.FullNode.Network);

                Transaction tx = this.node.FullNode.Network.CreateTransaction();
                tx.AddInput(new TxIn(new OutPoint(prevTrx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(this.node.MinerSecret.PubKey)));
                tx.AddOutput(new TxOut("5", dest.PubKey.Hash));
                tx.AddOutput(new TxOut("3", new Key().PubKey.Hash)); // 1 btc fee
                tx.Sign(this.node.FullNode.Network, this.node.MinerSecret, false);
                trxs.Add(tx);
            }
            var options = new ParallelOptions { MaxDegreeOfParallelism = transactionCount };
            Parallel.ForEach(trxs, options, transaction =>
            {
                this.node.Broadcast(transaction);
            });
        }

        private void CreateRegistrationTransactionAndBroadcast()
        {
            Transaction transaction = CreateRegistrationTransaction();
            IBroadcasterManager broadcasterManager = this.node.FullNode.NodeService<IBroadcasterManager>();
            broadcasterManager.BroadcastTransactionAsync(transaction).GetAwaiter().GetResult();
        }

        private Transaction CreateRegistrationTransaction()
        {
            var config = new ServiceNodeRegistrationConfig
            {
                ProtocolVersion = (int)ServiceNodeProtocolVersion.INITIAL,
                ServerId = this.serverSecret.GetAddress().ToString(),
                Ipv4Address = IPAddress.Parse("127.0.0.1"),
                Port = 37123,
                ConfigurationHash = "0123456789012345678901234567890123456789", // TODO hash of config file
                EcdsaPrivateKey = this.serverSecret,
            };

            RegistrationToken registrationToken = config.CreateRegistrationToken(this.network);

            IWalletTransactionHandler walletTransactionHandler = this.node.FullNode.NodeService<IWalletTransactionHandler>();
            Transaction transaction = TransactionUtils.BuildTransaction(
                this.network,
                walletTransactionHandler,
                config,
                registrationToken,
                WalletName,
                Account,
                Password,
                new RsaKey());

            return transaction;
        }

        private void CreateTransactionAndBroadcast(Money amount, BitcoinSecret dest = null)
        {
            const int txSrcBlockIndex = 2;
            CreateTransactionAndBroadcast(txSrcBlockIndex, amount.ToString(), dest);
        }

        private void CreateTransactionAndBroadcast(int blockIndex, string amount, BitcoinSecret dest = null)
        {
            dest = (dest == null) ? new BitcoinSecret(new Key(), this.node.FullNode.Network) : dest;

            Block block = this.node.FullNode.BlockStore().GetBlock(this.node.FullNode.ChainIndexer.GetHeader(blockIndex).HashBlock);
            Transaction prevTrx = block.Transactions.First();
            Transaction transaction = this.node.FullNode.Network.CreateTransaction();
            transaction.AddInput(new TxIn(new OutPoint(prevTrx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(this.node.MinerSecret.PubKey)));
            transaction.AddOutput(new TxOut(amount, dest.PubKey.Hash));
            transaction.AddOutput(new TxOut("1", new Key().PubKey.Hash));
            transaction.Sign(this.node.FullNode.Network, this.node.MinerSecret, false);
            this.node.Broadcast(transaction);
        }

        private BitcoinSecret GetWalletPrivateKeyForServer()
        {
            IWalletManager wm = this.node.FullNode.NodeService<IWalletManager>();

            Wallet wallet = wm.LoadWallet(Password, WalletName);
            HdAddress hdAddress = wallet.GetAllAddresses().Last();
            ISecret extendedPrivateKey = wallet.GetExtendedPrivateKeyForAddress(Password, hdAddress);
            return extendedPrivateKey.PrivateKey.GetBitcoinSecret(this.network);
        }
    }
}
