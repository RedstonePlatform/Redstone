using System;
using System.Net;
using NBitcoin;
using Redstone.Core.Networks;
using Redstone.ServiceNode;
using Redstone.ServiceNode.Events;
using Redstone.ServiceNode.Models;
using Redstone.ServiceNode.Utils;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Stratis.Bitcoin.Signals;
using Xunit;

namespace Redstone.Feature.ServiceNode.Tests
{
    public class ServiceNodeManagerTests
    {
        [Fact]
        public void ServiceNodeManagerTest()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                var node = builder.CreateRedstonePosNode(RedstoneNetworks.RegTest).Start();

                var rsa = new RsaKey();
                BitcoinSecret ecdsa = new Key().GetBitcoinSecret(RedstoneNetworks.Main);

                var address = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
                var token = new RegistrationToken(255,
                                                  address,
                                                  IPAddress.Parse("127.0.0.1"),
                                                  IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
                                                  "0123456789ABCDEF",
                                                  "",
                                                  37123,
                                                  ecdsa.PubKey);

                var cryptoUtils = new CryptoUtils(rsa, ecdsa);
                token.RsaSignature = cryptoUtils.SignDataRSA(token.GetHeaderBytes().ToArray());
                token.EcdsaSignature = cryptoUtils.SignDataECDSA(token.GetHeaderBytes().ToArray());

                RegistrationRecord record = new RegistrationRecord(DateTime.Now,
                                                                   Guid.NewGuid(),
                                                                   "137cc93a23383252348de58e53e193db471099e78ef89edf1fdea6ff340a7261",
                                                                   "0100000027c16c59051604707fcfc75e7ab33decbe72e72baff44ad2b38b78a13929eb5d092df004b20200000048473044022067c3c50f13a1b68402549c6a049a9298d8de03e176909cc6b44f9fd9d9db532502203e7416233a125264aef46698b22f429f60be9ed3b2115ec24f0a05de2a4e4cd901feffffff1d92cb2776b3c3cb3e89ad84c38c72fc72646238d23312a6b1931e7980f90f590200000049483045022100d4141e26371a198fb22347e0cc64fd0563ee11640108836e5f427873e4aa0d00022012960447890e151b46a19c2d1d480f7e81e5ceb615c9566be03189108cb08ce401feffffffa23c4e0da07acb439735e6f2a3c7a1ee15521138cc3815fdbac428e1756a3976010000004948304502210083dfda6e8a7584741c79d60a8c69c372789eb28680fdf43fe15e67eb05c7628e022041cde42e79e83c5be5902c035abcde02579e1841473efbbeb5a909d4520a8ea801fefffffff3b56cf319b4b4895beae1e047d77fddcfd8b86a3964226c0d24dda83335e40802000000484730440220219623b855cd1ac0ad43cb3b76097cb52db2d37b4a6a768a5911dfa3571ff4b602203eb843072f746e225e6c7225438f4f4bf012c18f1a1572d4ead288190d8d61cb01feffffff15209f56f6f5d1246f43057ab546be192659a3c88365529667eb3aa364ed4392000000006b483045022100a5791707155d03fb6e770c0dd0924cf08a3bd3b6e0b5713d7d3e84dc5c2e18bc02200370afce15733f5139a018d3ffc5e594e646ce372274818d9780906f9fb3699101210344e875df3990bf55d7218020b09aea6e1383206ec88344847771b3bc0d72251bfeffffff0220188fba000000001976a914b88f742a0a07af27ccfe21de8a40b9f7541f3e0088ac00e87648170000001976a914db0be998354d2139b14e06459d295de03b94fadb88ac90920000",
                                                                   token,
                                                                   null);

                var serviceNodeManager = node.FullNode.NodeService<IServiceNodeManager>();
                var signals = node.FullNode.NodeService<ISignals>();
                var signalsCount = 0;
                signals.Subscribe<ServiceNodeAdded>(a => { signalsCount++; });
                signals.Subscribe<ServiceNodeRemoved>(r => { signalsCount--; });

                var serviceNode = new Redstone.ServiceNode.Models.ServiceNode(record, address);
                serviceNodeManager.AddServiceNode(serviceNode);

                Assert.Equal(1, signalsCount);
                Assert.Equal(signalsCount, serviceNodeManager.GetServiceNodes().Count);

                serviceNodeManager.RemoveServiceNode(serviceNode);

                Assert.Equal(0, signalsCount);
                Assert.Equal(signalsCount, serviceNodeManager.GetServiceNodes().Count);
            }
        }
    }
}
