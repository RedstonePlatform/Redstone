using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NBitcoin.Protocol;

namespace NBitcoin.Networks
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
            messageStart[2] = 0x21;
            messageStart[3] = 0x11;
            uint magic = BitConverter.ToUInt32(messageStart, 0); // 0x5223570;

            this.Name = "RedstoneTest";
            this.Magic = magic;
            this.DefaultPort = 19156;
            this.RPCPort = 19157;
            this.CoinTicker = "TXRD";

            this.Consensus.PowLimit = new Target(new uint256("0000ffff00000000000000000000000000000000000000000000000000000000"));
            this.Consensus.DefaultAssumeValid = new uint256("0x98fa6ef0bca5b431f15fd79dc6f879dc45b83ed4b1bbe933a383ef438321958e"); // 372652
            this.Consensus.CoinbaseMaturity = 10;

            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (65) };
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (65 + 128) };

            this.Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                //{ 0, new CheckpointInfo(new uint256("0x00000e246d7b73b88c9ab55f2e5e94d9e22d471def3df5ea448f5576b1d156b9"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                //{ 163000, new CheckpointInfo(new uint256("0x4e44a9e0119a2e7cbf15e570a3c649a5605baa601d953a465b5ebd1c1982212a"), new uint256("0x0646fc7db8f3426eb209e1228c7d82724faa46a060f5bbbd546683ef30be245c")) },
            };

            // TODO:Redstone - need seed 
            this.DNSSeeds = new List<DNSSeedData>()
                /*
                {
                    //new DNSSeedData("seednode1.stratisplatform.com", "seednode1.stratisplatform.com"),
                    //new DNSSeedData("seednode2.stratis.cloud", "seednode2.stratis.cloud"),
                    //new DNSSeedData("seednode3.stratisplatform.com", "seednode3.stratisplatform.com"),
                    //new DNSSeedData("seednode4.stratis.cloud", "seednode4.stratis.cloud")
                })*/;

            this.SeedNodes = this.ConvertToNetworkAddresses(new string[] { /*"35.176.127.127", "35.176.127.127"*/}, this.DefaultPort).ToList();

            // Create the genesis block.
            this.GenesisTime = 1470467000;
            this.GenesisNonce = 1831645;
            this.GenesisBits = 0x1e0fffff;
            this.GenesisVersion = 1;
            this.GenesisReward = Money.Zero;

            this.Genesis = CreateRedstoneGenesisBlock(this.Consensus.ConsensusFactory, this.GenesisTime, this.GenesisNonce, this.GenesisBits, this.GenesisVersion, this.GenesisReward);
            this.Genesis.Header.Time = 1493909211;
            this.Genesis.Header.Nonce = 2433759;
            this.Genesis.Header.Bits = this.Consensus.PowLimit;
            this.Consensus.HashGenesisBlock = this.Genesis.GetHash();
            Assert(this.Consensus.HashGenesisBlock == uint256.Parse("5166f378d33b357de3a84575e8ac27f86d62c93766bfc275076fdc7926e6ccb3"));
        }
    }
}
