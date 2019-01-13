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
        public void RegistrationTest()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                CoreNode node1 = builder.CreateRedstonePosNode(network).WithWallet(Password, WalletName, Passphrase).Start();
                CoreNode node2 = builder.CreateRedstonePosNode(network).Start();

                int maturity = (int)network.Consensus.CoinbaseMaturity;
                TestHelper.MineBlocks(node1, maturity + 5, true, WalletName, Password);
                //TestHelper.ConnectAndSync(node1, node2);

                CreateTransactionsAndBroadcast(node1, 4);
                TestHelper.MineBlocks(node1, 1, true, WalletName, Password);
                //TestHelper.ConnectAndSync(node1, node2);

                CreateRegistrationTransactionAndBroadcast(node1, 5, Password);
                TestHelper.MineBlocks(node1, 1, true, WalletName, Password);
                TestHelper.ConnectAndSync(node1, node2);

                //TestHelper.WaitForNodeToSync(node1, node2);

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

        private static void CreateRegistrationTransactionAndBroadcast(CoreNode node1, int index, string walletPassword)
        {
            var outputAmount = new Money(0.0123m, MoneyUnit.BTC);
            Block block = node1.FullNode.BlockStore().GetBlockAsync(node1.FullNode.Chain.GetBlock(index).HashBlock).Result;
            Transaction prevTrx = block.Transactions.First();

            var redstonemarker = Encoding.UTF8.GetBytes("REDSTONE_SN_REGISTRATION_MARKER");
            var scriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(redstonemarker);

            Transaction tx = node1.FullNode.Network.CreateTransaction();
            tx.AddInput(new TxIn(new OutPoint(prevTrx.GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(node1.MinerSecret.PubKey)));
            tx.AddOutput(new TxOut(outputAmount, scriptPubKey.Hash));


            var rsa = new RsaKey();
            var ecdsa = new BitcoinSecret(new Key(), node1.FullNode.Network);
            var serverAddress = ecdsa.GetAddress().ToString();

            var token = new RegistrationToken(1,
                serverAddress,
                IPAddress.Parse("127.0.0.1"),
                IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
                "0123456789ABCDEF",
                "",
                37123,
                ecdsa.PubKey);

            var cryptoUtils = new CryptoUtils(rsa, ecdsa);
            token.RsaSignature = cryptoUtils.SignDataRSA(token.GetHeaderBytes().ToArray());
            token.EcdsaSignature = cryptoUtils.SignDataECDSA(token.GetHeaderBytes().ToArray());

            byte[] msgBytes = token.GetRegistrationTokenBytes(rsa, ecdsa);

            var dest = new BitcoinSecret(new Key(), node1.FullNode.Network);

            foreach (PubKey pubKey in BlockChainDataConversions.BytesToPubKeys(msgBytes))
            {
                tx.AddOutput(new TxOut(outputAmount, pubKey.ScriptPubKey));
            }

            tx.Sign(node1.FullNode.Network, node1.MinerSecret, false);
            node1.Broadcast(tx);
        }

        private static void CreateRegistrationTransactionsAndBroadcast(CoreNode node1, string walletPassword)
        {
            var rsa = new RsaKey();
            var ecdsa = new BitcoinSecret(new Key(), node1.FullNode.Network);
            var serverAddress = ecdsa.GetAddress().ToString();

            //var token = new RegistrationToken(1,
            //    serverAddress,
            //    IPAddress.Parse("127.0.0.1"),
            //    IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
            //    "0123456789ABCDEF",
            //    "",
            //    37123,
            //    ecdsa.PubKey);

            //var cryptoUtils = new CryptoUtils(rsa, ecdsa);
            //token.RsaSignature = cryptoUtils.SignDataRSA(token.GetHeaderBytes().ToArray());
            //token.EcdsaSignature = cryptoUtils.SignDataECDSA(token.GetHeaderBytes().ToArray());

            //byte[] msgBytes = token.GetRegistrationTokenBytes(rsa, ecdsa);

            //Block tipBlock = node1.FullNode.Chain.Tip.Block;
            //Transaction prevTrx = tipBlock.Transactions.First();
            //var dest = new BitcoinSecret(new Key(), node1.FullNode.Network);

            var outputAmount = new Money(0.99m, MoneyUnit.BTC);
            var redstonemarker = Encoding.UTF8.GetBytes("REDSTONE_SN_REGISTRATION_MARKER");

            var recipients = new List<Recipient>
            {
                new Recipient
                {
                    Amount = outputAmount,
                    ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(redstonemarker)
                }
            };

            //foreach (PubKey pubKey in BlockChainDataConversions.BytesToPubKeys(msgBytes))
            //{
            //    recipients.Add(new Recipient
            //    {
            //        Amount = outputAmount,
            //        ScriptPubKey = pubKey.ScriptPubKey
            //    });
            //}

            var wm1 = node1.FullNode.NodeService<IWalletManager>() as WalletManager;
            var walletReference = new WalletAccountReference()
            {
                // Default to the first wallet & first account
                AccountName = wm1.Wallets.First().GetAccountsByCoinType((CoinType)node1.FullNode.Network.Consensus.CoinType).First().Name,
                WalletName = wm1.Wallets.First().Name
            };

            var context = new TransactionBuildContext(RedstoneNetworks.RegTest)
            {
                Recipients = recipients,
                AccountReference = walletReference,
                MinConfirmations = 0,
                OverrideFeeRate = new FeeRate(new Money(0.0033m, MoneyUnit.BTC)),
                Shuffle = false,
                WalletPassword = walletPassword,
                Sign = true,
            };

            var wth1 = node1.FullNode.NodeService<IWalletTransactionHandler>() as WalletTransactionHandler;
            var transaction = wth1.BuildTransaction(context);
            node1.Broadcast(transaction);
        }


        /* 
         * 
        [Fact]
        public async Task RegistrationTest()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                CoreNode node1 = builder.CreateRedstonePosNode(RedstoneNetworks.RegTest).WithWallet().Start();
                CoreNode node2 = builder.CreateRedstonePosNode(RedstoneNetworks.RegTest).WithWallet().Start();
                node1.Start();
                node2.Start();
                node1.NotInIBD();
                node2.NotInIBD();

                var rpc1 = node1.CreateRPCClient();
                var rpc2 = node2.CreateRPCClient();

                // addnode RPC call does not seem to work, so connect directly
                node1.FullNode.ConnectionManager.AddNodeAddress(node2.Endpoint);

                // Create the originating node's wallet
                var wm1 = node1.FullNode.NodeService<IWalletManager>() as WalletManager;
                wm1.CreateWallet("Registration1", "registration", "Registration1");

                var wallet1 = wm1.GetWalletByName("registration");
                var account1 = wallet1.GetAccountsByCoinType((CoinType)node1.FullNode.Network.Consensus.CoinType).First();
                var address1 = account1.GetFirstUnusedReceivingAddress();
                var secret1 = wallet1.GetExtendedPrivateKeyForAddress("Registration1", address1);
                node1.SetDummyMinerSecret(new BitcoinSecret(secret1.PrivateKey, node1.FullNode.Network));

                // Generate a block so we have some funds to create a transaction with

                node1.GenerateRedstoneWithMiner(4);

                TestHelper.TriggerSync(node1);
                TestHelper.TriggerSync(node2);

                TestHelper.WaitLoop(() =>
                {
                    var rpc1Hash = rpc1.GetBestBlockHash();
                    var rpc2Hash = rpc2.GetBestBlockHash();
                    return rpc1Hash == rpc2Hash;
                });

                var rsa = new RsaKey();
                var ecdsa = new Key().GetBitcoinSecret(RedstoneNetworks.RegTest);
                var serverAddress = ecdsa.GetAddress().ToString();

                var token = new RegistrationToken(1,
                    serverAddress,
                    IPAddress.Parse("127.0.0.1"),
                    IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
                    "0123456789ABCDEF",
                    "",
                    37123,
                    ecdsa.PubKey);

                var cryptoUtils = new CryptoUtils(rsa, ecdsa);
                token.RsaSignature = cryptoUtils.SignDataRSA(token.GetHeaderBytes().ToArray());
                token.EcdsaSignature = cryptoUtils.SignDataECDSA(token.GetHeaderBytes().ToArray());

                byte[] msgBytes = token.GetRegistrationTokenBytes(rsa, ecdsa);

                Transaction sendTx = new Transaction();
                Money outputValue = new Money(0.01m, MoneyUnit.BTC);
                Money feeValue = new Money(0.01m, MoneyUnit.BTC);

                byte[] bytes = Encoding.UTF8.GetBytes("REDSTONE_SN_REGISTRATION_MARKER");
                sendTx.Outputs.Add(new TxOut()
                {
                    Value = outputValue,
                    ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(bytes)
                });

                foreach (PubKey pubKey in BlockChainDataConversions.BytesToPubKeys(msgBytes))
                {
                    TxOut destTxOut = new TxOut()
                    {
                        Value = outputValue,
                        ScriptPubKey = pubKey.ScriptPubKey
                    };

                    sendTx.Outputs.Add(destTxOut);
                }

                var wth1 = node1.FullNode.NodeService<IWalletTransactionHandler>() as WalletTransactionHandler;

                List<Recipient> recipients = new List<Recipient>();

                foreach (TxOut txOut in sendTx.Outputs)
                {
                    recipients.Add(new Recipient() { Amount = txOut.Value, ScriptPubKey = txOut.ScriptPubKey });
                }

                var walletReference = new WalletAccountReference()
                {
                    // Default to the first wallet & first account
                    AccountName = wm1.Wallets.First().GetAccountsByCoinType((CoinType)node1.FullNode.Network.Consensus.CoinType).First().Name,
                    WalletName = wm1.Wallets.First().Name
                };

                var context = new TransactionBuildContext(RedstoneNetworks.RegTest)
                {
                    Recipients = recipients,
                    AccountReference = walletReference,
                    MinConfirmations = 0,
                    OverrideFeeRate = new FeeRate(new Money(0.001m, MoneyUnit.BTC)),
                    Shuffle = false,
                    WalletPassword = "Registration1",
                    Sign = true,

                };

                var tx = wth1.BuildTransaction(context);


                var broadcaster = node1.FullNode.NodeService<IBroadcasterManager>();
                broadcaster.BroadcastTransactionAsync(tx).GetAwaiter().GetResult();

                TestHelper.WaitLoop(() => rpc1.GetRawMempool().Length > 0);

                node1.GenerateRedstoneWithMiner(1);

                Thread.Sleep(10000);


                TestHelper.TriggerSync(node1);
                TestHelper.TriggerSync(node2);


                TestHelper.WaitLoop(() => rpc1.GetBestBlockHash() == rpc2.GetBestBlockHash());

                Console.WriteLine("Checking if registration was received...");

                var rm2 = node2.FullNode.NodeService<RegistrationManager>();
                var rs2 = rm2.GetRegistrationStore();

                foreach (var record in rs2.GetAll())
                {
                    Console.WriteLine("Received registration: " + record.RecordTxId);
                }

                Console.WriteLine(rs2.GetAll().Count);

                Thread.Sleep(10000);

                node1.Kill();
                node2.Kill();
            }
        }

        [Fact]
        public async Task MinimalTest()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            // TODO: AC-FromBreeze this was called with no params
            {
                // TODO: AC-FromBreeze check these calls for difference in network build
                CoreNode node1 = builder.CreateRedstonePosNode(RedstoneNetworks.RegTest);
                node1.Start();
                node1.NotInIBD();

                var rpc1 = node1.CreateRPCClient();

                // Create the originating node's wallet
                var wm1 = node1.FullNode.NodeService<IWalletManager>() as WalletManager;
                wm1.CreateWallet("Registration1", "registration", "Registration1");

                var wallet1 = wm1.GetWalletByName("registration");
                var account1 = wallet1.GetAccountsByCoinType((CoinType)node1.FullNode.Network.Consensus.CoinType).First();
                var address1 = account1.GetFirstUnusedReceivingAddress();
                var secret1 = wallet1.GetExtendedPrivateKeyForAddress("Registration1", address1);

                // We can use SetDummyMinerSecret here because the private key is already in the wallet
                node1.SetDummyMinerSecret(new BitcoinSecret(secret1.PrivateKey, node1.FullNode.Network));

                // Generate a block so we have some funds to create a transaction with
                node1.GenerateRedstoneWithMiner(10);

                Thread.Sleep(20000);

                node1.GenerateRedstoneWithMiner(10);

                Thread.Sleep(20000);

                node1.GenerateRedstoneWithMiner(10);

                Thread.Sleep(20000);

                node1.Kill();
            }
        }

        */
    }
}
