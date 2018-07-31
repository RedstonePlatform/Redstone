using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;

namespace Redstone.Core.Networks
{
    public class RedstoneTest : RedstoneMain
    {
        public RedstoneTest()
        {
            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0x71;
            messageStart[1] = 0x31;
            messageStart[2] = 0x23;
            messageStart[3] = 0x11;
            uint magic = BitConverter.ToUInt32(messageStart, 0); // 0x5223570;

            this.Name = "RedstoneTest";
            this.Magic = magic;
            this.DefaultPort = 19156;
            this.RPCPort = 19157;
            this.CoinTicker = "TXRD";

            this.Consensus.PowLimit = new Target(new uint256("0000ffff00000000000000000000000000000000000000000000000000000000"));
            this.Consensus.ProofOfStakeLimit = new BigInteger(uint256.Parse("0000ffff00000000000000000000000000000000000000000000000000000000").ToBytes(false));
            this.Consensus.ProofOfStakeLimitV2 = new BigInteger(uint256.Parse("0000ffff00000000000000000000000000000000000000000000000000000000").ToBytes(false));
            this.Consensus.DefaultAssumeValid = new uint256("0x98fa6ef0bca5b431f15fd79dc6f879dc45b83ed4b1bbe933a383ef438321958e"); // 372652
            this.Consensus.CoinbaseMaturity = 10;

            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (65) };
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (65 + 128) };

            this.Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                //{ 0, new CheckpointInfo(new uint256("0x5166f378d33b357de3a84575e8ac27f86d62c93766bfc275076fdc7926e6ccb3"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                //{ 2, new CheckpointInfo(new uint256("0xff24fef45f00088ef09b713d24adc07494bedf69d93645600b76debbd38cbedf"), new uint256("0x7d61c139a471821caa6b7635a4636e90afcfe5e195040aecbc1ad7d24924db1e")) }, // Premine
                //{ 261, new CheckpointInfo(new uint256("0xfde037496468d67c1e0b76656ccfc90d2a4b8b489c7b05599de7ae58d85c10f2"), new uint256("0x7d61c139a471821caa6b7635a4636e90afcfe5e195040aecbc1ad7d24924db1e")) },

                
            };

            this.DNSSeeds = new List<DNSSeedData>()
            {
                //new DNSSeedData("seednode1", "18.130.175.184"),
            };

            this.SeedNodes = this.ConvertToNetworkAddresses(new string[]
            {
                "80.211.88.189","80.211.88.201", "80.211.88.233", "80.211.88.244"
            }, this.DefaultPort).ToList();

            // Create the genesis block.
            this.GenesisTime = 1530256857;
            this.GenesisNonce = 1349369;
            this.GenesisBits = this.Consensus.PowLimit;
            this.GenesisVersion = 1;
            this.GenesisReward = Money.Zero;

            this.Genesis = CreateRedstoneGenesisBlock(this.Consensus.ConsensusFactory, this.GenesisTime, this.GenesisNonce, this.GenesisBits, this.GenesisVersion, this.GenesisReward);
            this.Consensus.HashGenesisBlock = this.Genesis.GetHash();
            Assert(this.Consensus.HashGenesisBlock == uint256.Parse("0ecac183d0f31c87aee57f8fd0a49a9ac185ce0a9f649c777823180ebf7efe2a"));
        }
    }
}
