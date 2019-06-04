using System.Collections.Generic;
using System.Linq;
using System.Net;
using NBitcoin;
using Redstone.Core.Networks;
using Redstone.Features.ServiceNode;
using Redstone.ServiceNode;
using Redstone.ServiceNode.Models;
using Stratis.Bitcoin.Features.BlockStore.AddressIndexing;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.IntegrationTests.Common;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Redstone.Feature.ServiceNode.Tests
{
    public class ServiceNodeRegistrationTests
    {
        private readonly ITestOutputHelper output;
        private const string Password = "RegistrationWallet1Password";
        private const string WalletName = "RegistrationWallet1";
        private const string Passphrase = "RegistrationWallet1Passphrase";
        private const string Account = "account 0";
        private readonly Network network = RedstoneNetworks.RegTest;
        private CoreNode node;
        private CoreNode watchingNode;
        private IWalletTransactionHandler walletTransactionHandler;
        private IWalletManager walletManager;
        private IServiceNodeManager serviceNodeManager;
        private IAddressIndexer addressIndexer;
        private IServiceNodeManager watchingServiceNodeManager;
        private IAddressIndexer watchingAddressIndexer;
        private BitcoinSecret serverSecret;
        private readonly Money collateral;
        private readonly int maturity;

        public ServiceNodeRegistrationTests(ITestOutputHelper output)
        {
            this.collateral = new Money(this.network.Consensus.ServiceNodeCollateralThreshold, MoneyUnit.BTC);
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

                CreateTransactionAndBroadcast(new Money(5, MoneyUnit.BTC));
                TestHelper.MineBlocks(this.node, 1, true, WalletName, Password);

                var serverSecret = new BitcoinSecret(new Key(), this.node.FullNode.Network);
                Transaction transaction = CreateRegistrationTransaction();

                var registrationToken = new RegistrationToken();
                registrationToken.ParseTransaction(transaction, this.network);
                Assert.True(registrationToken.Validate(this.network));
            }
        }

        [Fact]
        public void RegistrationWithFullCollateralShouldPersist()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                Setup(builder);

                // Seed 
                MineConnectAndSync(this.maturity + 1);
                AssertNodeCountAndBalance(0, new Money(0, MoneyUnit.BTC));

                // Register
                CreateRegistrationTransactionAndBroadcast();
                MineConnectAndSync();
                AssertNodeCountAndBalance(1, new Money(0, MoneyUnit.BTC));

                // Pay Collateral
                CreateTransactionAndBroadcast(this.collateral, this.serverSecret);
                MineConnectAndSync();
                AssertNodeCountAndBalance(1, this.collateral);

                // Transactions up to collateral block period + 1
                for (int i = 0; i < this.network.Consensus.ServiceNodeCollateralBlockPeriod; i++)
                {
                    BuildTransactionAndBroadcast(new Money(6 + i, MoneyUnit.BTC));
                    MineConnectAndSync();
                    AssertNodeCountAndBalance(1, this.collateral);
                }
            }
        }

        [Fact]
        public void RegistrationWithoutFullCollateralShouldNotPersist()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                Setup(builder);

                // Seed 
                MineConnectAndSync(this.maturity + 1);
                AssertNodeCountAndBalance(0, new Money(0, MoneyUnit.BTC));

                // Register
                CreateRegistrationTransactionAndBroadcast();
                MineConnectAndSync();
                AssertNodeCountAndBalance(1, new Money(0, MoneyUnit.BTC));

                Money collateral = this.collateral - 1;

                // Pay Collateral
                CreateTransactionAndBroadcast(collateral, this.serverSecret);
                MineConnectAndSync();
                AssertNodeCountAndBalance(1, collateral);

                // Transactions up to collateral block period
                for (int i = 0; i < this.network.Consensus.ServiceNodeCollateralBlockPeriod - 1; i++)
                {
                    BuildTransactionAndBroadcast(new Money(6 + i, MoneyUnit.BTC));
                    MineConnectAndSync();
                    AssertNodeCountAndBalance(1, collateral);
                }

                BuildTransactionAndBroadcast(new Money(5, MoneyUnit.BTC));
                MineConnectAndSync();
                AssertNodeCountAndBalance(0, collateral);
            }
        }

        private void Setup(NodeBuilder builder)
        {
            this.node = builder.CreateRedstonePosNode(this.network)
                                .WithWallet(Password, WalletName, Passphrase)
                                .Start();
            this.watchingNode = builder.CreateRedstonePosNode(this.network).Start();

            this.walletTransactionHandler = this.node.FullNode.NodeService<IWalletTransactionHandler>();
            this.walletManager = this.node.FullNode.NodeService<IWalletManager>();

            this.serviceNodeManager = this.node.FullNode.NodeService<IServiceNodeManager>();
            this.addressIndexer = this.node.FullNode.NodeService<IAddressIndexer>();

            this.watchingServiceNodeManager = this.watchingNode.FullNode.NodeService<IServiceNodeManager>();
            this.watchingAddressIndexer = this.watchingNode.FullNode.NodeService<IAddressIndexer>();

            this.serverSecret = this.GetWalletPrivateKeyForServer();
        }
        private BitcoinSecret GetWalletPrivateKeyForServer()
        {
            IWalletManager wm = this.node.FullNode.NodeService<IWalletManager>();

            Wallet wallet = wm.LoadWallet(Password, WalletName);
            HdAddress hdAddress = wallet.GetAllAddresses().Last();
            ISecret extendedPrivateKey = wallet.GetExtendedPrivateKeyForAddress(Password, hdAddress);
            return extendedPrivateKey.PrivateKey.GetBitcoinSecret(this.network);
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

        private void AssertNodeCountAndBalance(int expectedNodeCount, Money expectedBalance)
        {
            Money balance = this.addressIndexer.GetAddressBalance(this.serverSecret.GetAddress().ToString()) ?? 0;
            List<IServiceNode> nodes = this.serviceNodeManager.GetServiceNodes();

            Money watchingBalance = this.watchingAddressIndexer.GetAddressBalance(this.serverSecret.GetAddress().ToString()) ?? 0;
            List<IServiceNode> watchingNodes = this.watchingServiceNodeManager.GetServiceNodes();

            this.output.WriteLine($"Expected {expectedBalance?.ToUnit(MoneyUnit.BTC)} | Balance: {balance?.ToUnit(MoneyUnit.BTC)}");
            this.output.WriteLine($"Expected {expectedBalance?.ToUnit(MoneyUnit.BTC)} | WatchingBalance: {watchingBalance?.ToUnit(MoneyUnit.BTC)}");

            this.output.WriteLine($"Expected {expectedNodeCount} | Actual StoreCount {nodes.Count}");
            this.output.WriteLine($"Expected {expectedNodeCount} | Actual StoreCount {watchingNodes.Count}");

            this.output.WriteLine("-------------------------------------------------------------------");

            Assert.Equal(expectedBalance, balance);
            Assert.Equal(expectedBalance, watchingBalance);
            Assert.Equal(expectedNodeCount, nodes.Count);
            Assert.Equal(expectedNodeCount, watchingNodes.Count);
        }

        private void CreateTransactionAndBroadcast(Money amount, BitcoinSecret dest = null)
        {
            Transaction transaction = CreateTransaction(amount, dest);
            BroadcastTransaction(transaction);
        }

        private void BuildTransactionAndBroadcast(Money amount, BitcoinSecret dest = null)
        {
            Transaction transaction = BuildTransaction(amount, dest);
            BroadcastTransaction(transaction);
        }

        private void CreateRegistrationTransactionAndBroadcast()
        {
            Transaction transaction = CreateRegistrationTransaction();
            BroadcastTransaction(transaction);
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
                this.walletManager,
                config,
                registrationToken,
                WalletName,
                Account,
                Password,
                new RsaKey());

            return transaction;
        }
        private Transaction CreateTransaction(Money amount, BitcoinSecret dest = null)
        {
            dest = (dest == null) ? new BitcoinSecret(new Key(), this.node.FullNode.Network) : dest;

            //var inBlockHeight = this.node.FullNode.ChainIndexer.Tip.Height - (int)this.network.Consensus.CoinbaseMaturity;
            Block inBlock = this.node.FullNode.BlockStore().GetBlock(this.node.FullNode.ChainIndexer.GetHeader(2).HashBlock);
            Transaction inTrx = inBlock.Transactions.First();
            Transaction transaction = this.node.FullNode.Network.CreateTransaction();
            transaction.AddInput(new TxIn(new OutPoint(inTrx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(this.node.MinerSecret.PubKey)));
            transaction.AddOutput(new TxOut(amount, dest.PubKey.Hash));
            transaction.AddOutput(new TxOut(new Money(1, MoneyUnit.BTC), new Key().PubKey.Hash));
            transaction.Sign(this.node.FullNode.Network, this.node.MinerSecret, false);
            return transaction;
        }
        private void BroadcastTransaction(Transaction transaction)
        {
            IBroadcasterManager broadcasterManager = this.node.FullNode.NodeService<IBroadcasterManager>();
            broadcasterManager.BroadcastTransactionAsync(transaction).GetAwaiter().GetResult();
        }
        private Transaction BuildTransaction(Money amount, BitcoinSecret dest)
        {
            dest = (dest == null) ? new BitcoinSecret(new Key(), this.node.FullNode.Network) : dest;

            var recipient = new Recipient
            {
                Amount = amount,
                ScriptPubKey = dest.ScriptPubKey
            };

            var accountReference = new WalletAccountReference()
            {
                AccountName = Account,
                WalletName = WalletName
            };

            var context = new TransactionBuildContext(this.node.FullNode.Network)
            {
                AccountReference = accountReference,
                Recipients = new[] { recipient }.ToList(),
                Shuffle = false,
                Sign = true,
                FeeType = FeeType.High,
                //OverrideFeeRate = new FeeRate(new Money(1, MoneyUnit.BTC)),
                WalletPassword = Password,
            };
            context.TransactionBuilder.CoinSelector = new DefaultCoinSelector
            {
                GroupByScriptPubKey = false
            };
            Transaction transaction = this.walletTransactionHandler.BuildTransaction(context);

            return transaction;
        }
    }
}
