using System;
using System.Collections.Generic;
using System.Net;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin
{
    public partial class Network
    {
        //static RedstoneNetwork()
        //{
        //    // initialize the networks
        //    bool saveTS = Transaction.TimeStamp;
        //    bool saveSig = Block.BlockSignature;
        //    Transaction.TimeStamp = false;
        //    Block.BlockSignature = false;

        //    Network main = RedstoneMain;
        //    Network testNet = RedstoneTest;
        //    Network regTest = RedstoneRegTest;

        //    Transaction.TimeStamp = saveTS;
        //    Block.BlockSignature = saveSig;
        //}

        /// <summary> Redstone maximal value for the calculated time offset. If the value is over this limit, the time syncing feature will be switched off. </summary>
        public const int RedstoneMaxTimeOffsetSeconds = 25 * 60;

        /// <summary> Stratis default value for the maximum tip age in seconds to consider the node in initial block download (2 hours). </summary>
        public const int RedstoneDefaultMaxTipAgeInSeconds = 2 * 60 * 60;

        /// <summary> The name of the root folder containing the different Redstone blockchains (RedstoneMain, RedstoneTest, RedstoneRegTest). </summary>
        public const string RedstoneRootFolderName = "redstone";

        /// <summary> The default name used for the Redstone configuration file. </summary>
        public const string RedstoneDefaultConfigFilename = "redstone.conf";

        public static Network RedstoneMain => Network.GetNetwork("RedstoneMain") ?? InitRedstoneMain();

        public static Network RedstoneTest => Network.GetNetwork("RedstoneTest") ?? InitRedstoneTest();

        public static Network RedstoneRegTest => Network.GetNetwork("RedstoneRegTest") ?? InitRedstoneRegTest();

        private static Network InitRedstoneMain()
        {
            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0x70;
            messageStart[1] = 0x35;
            messageStart[2] = 0x22;
            messageStart[3] = 0x05;
            var magic = BitConverter.ToUInt32(messageStart, 0); //0x5223570; 

            Network network = new Network
            {
                Name = "RedstoneMain",
                RootFolderName = RedstoneRootFolderName,
                DefaultConfigFilename = RedstoneDefaultConfigFilename,
                Magic = magic,
                DefaultPort = 19056,
                RPCPort = 19057,
                MinTxFee = 10000,
                FallbackFee = 60000,
                MinRelayTxFee = 10000,
                MaxTimeOffsetSeconds = RedstoneMaxTimeOffsetSeconds,
                MaxTipAge = RedstoneDefaultMaxTipAgeInSeconds
            };

            network.Consensus.SubsidyHalvingInterval = 210000;
            network.Consensus.MajorityEnforceBlockUpgrade = 750;
            network.Consensus.MajorityRejectBlockOutdated = 950;
            network.Consensus.MajorityWindow = 1000;
            network.Consensus.BuriedDeployments[BuriedDeployments.BIP34] = 0;
            network.Consensus.BuriedDeployments[BuriedDeployments.BIP65] = 0;
            network.Consensus.BuriedDeployments[BuriedDeployments.BIP66] = 0;
            network.Consensus.BIP34Hash = new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8");
            network.Consensus.PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
            network.Consensus.PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
            network.Consensus.PowTargetSpacing = TimeSpan.FromSeconds(10 * 60);
            network.Consensus.PowAllowMinDifficultyBlocks = false;
            network.Consensus.PowNoRetargeting = false;
            network.Consensus.RuleChangeActivationThreshold = 1916; // 95% of 2016
            network.Consensus.MinerConfirmationWindow = 2016; // nPowTargetTimespan / nPowTargetSpacing
            network.Consensus.LastPOWBlock = 12500;
            network.Consensus.IsProofOfStake = true;
            network.Consensus.ConsensusFactory = new PosConsensusFactory() { Consensus = network.Consensus };
            network.Consensus.ProofOfStakeLimit = new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));
            network.Consensus.ProofOfStakeLimitV2 = new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));
            network.Consensus.CoinType = 787264; // unique coin type TODO how do we get this added
            network.Consensus.DefaultAssumeValid = new uint256("0x8c2cf95f9ca72e13c8c4cdf15c2d7cc49993946fb49be4be147e106d502f1869"); // 795970
            network.genesis = CreateRedstoneGenesisBlock(network.Consensus.ConsensusFactory, 1470467000, 1831645, 0x1e0fffff, 1, Money.Zero);
            network.Consensus.HashGenesisBlock = network.genesis.GetHash();
            Assert(network.Consensus.HashGenesisBlock == uint256.Parse("f0df827e7b388fc76cfe479abf10b7ee2769a16a94d0d1c3b6432d74a796f1e3"));
            Assert(network.genesis.Header.HashMerkleRoot == uint256.Parse("76417fee12594f59b7a15a7811b562736677557ec68aef76c5c758440017fb49"));

            network.Checkpoints = new Dictionary<int, CheckpointInfo>
            {
            };

            network.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (63) };
            network.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (125) };
            network.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (63 + 128) };
            network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            network.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            network.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            network.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            network.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            network.Base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2a };
            network.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };
            network.Base58Prefixes[(int)Base58Type.COLORED_ADDRESS] = new byte[] { 0x13 };

            var encoder = new Bech32Encoder("bc");
            network.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            network.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            // TODO:Redstone - need seed hosts
            network.DNSSeeds.AddRange(new[]
            {
                new DNSSeedData("35.176.127.127", "35.176.127.127"), // initial test node will need to be updated
                new DNSSeedData("seednode1.stratisplatform.com", "seednode1.stratisplatform.com"),
                new DNSSeedData("seednode2.stratis.cloud", "seednode2.stratis.cloud"),
                new DNSSeedData("seednode3.stratisplatform.com", "seednode3.stratisplatform.com"),
                new DNSSeedData("seednode4.stratis.cloud", "seednode4.stratis.cloud")
            });

            var seeds = new[] { "35.176.127.127", "35.176.127.127" };
            // Convert the seeds array into usable address objects.
            Random rand = new Random();
            TimeSpan oneWeek = TimeSpan.FromDays(7);
            foreach (string seed in seeds)
            {
                // It'll only connect to one or two seed nodes because once it connects,
                // it'll get a pile of addresses with newer timestamps.
                // Seed nodes are given a random 'last seen time' of between one and two weeks ago.
                NetworkAddress addr = new NetworkAddress
                {
                    Time = DateTime.UtcNow - (TimeSpan.FromSeconds(rand.NextDouble() * oneWeek.TotalSeconds)) - oneWeek,
                    Endpoint = Utils.ParseIpEndpoint(seed, network.DefaultPort)
                };

                network.SeedNodes.Add(addr);
            }

            Network.Register(network);
            return network;
        }

        private static Network InitRedstoneTest()
        {
            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0x71;
            messageStart[1] = 0x31;
            messageStart[2] = 0x21;
            messageStart[3] = 0x11;
            var magic = BitConverter.ToUInt32(messageStart, 0); // 0x5223570;

            Network network = new Network
            {
                Name = "RedstoneTest",
                RootFolderName = RedstoneRootFolderName,
                DefaultConfigFilename = RedstoneDefaultConfigFilename,
                Magic = magic,
                DefaultPort = 19156,
                RPCPort = 19157,
                MaxTimeOffsetSeconds = RedstoneMaxTimeOffsetSeconds,
                MaxTipAge = RedstoneDefaultMaxTipAgeInSeconds,
                MinTxFee = 10000,
                FallbackFee = 60000,
                MinRelayTxFee = 10000
            };

            network.Consensus.SubsidyHalvingInterval = 210000;
            network.Consensus.MajorityEnforceBlockUpgrade = 750;
            network.Consensus.MajorityRejectBlockOutdated = 950;
            network.Consensus.MajorityWindow = 1000;
            network.Consensus.BuriedDeployments[BuriedDeployments.BIP34] = 227931;
            network.Consensus.BuriedDeployments[BuriedDeployments.BIP65] = 388381;
            network.Consensus.BuriedDeployments[BuriedDeployments.BIP66] = 363725;
            network.Consensus.BIP34Hash = new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8");
            network.Consensus.PowLimit = new Target(new uint256("0000ffff00000000000000000000000000000000000000000000000000000000"));
            network.Consensus.PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
            network.Consensus.PowTargetSpacing = TimeSpan.FromSeconds(10 * 60);
            network.Consensus.PowAllowMinDifficultyBlocks = false;
            network.Consensus.PowNoRetargeting = false;
            network.Consensus.RuleChangeActivationThreshold = 1916; // 95% of 2016
            network.Consensus.MinerConfirmationWindow = 2016; // nPowTargetTimespan / nPowTargetSpacing
            network.Consensus.LastPOWBlock = 12500;
            network.Consensus.IsProofOfStake = false;
            network.Consensus.ConsensusFactory = new PosConsensusFactory() { Consensus = network.Consensus };
            network.Consensus.ProofOfStakeLimit = new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));
            network.Consensus.ProofOfStakeLimitV2 = new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));
            network.Consensus.CoinType = 1001;
            network.Consensus.DefaultAssumeValid = new uint256("0x12ae16993ce7f0836678f225b2f4b38154fa923bd1888f7490051ddaf4e9b7fa"); // 372652

            Block genesis = CreateRedstoneGenesisBlock(network.Consensus.ConsensusFactory, 1470467000, 1831645, 0x1e0fffff, 1, Money.Zero);
            genesis.Header.Time = 1493909211;
            genesis.Header.Nonce = 2433759;
            genesis.Header.Bits = network.Consensus.PowLimit;
            network.genesis = genesis;
            network.Consensus.HashGenesisBlock = network.genesis.GetHash();
            Assert(network.Consensus.HashGenesisBlock == uint256.Parse("5166f378d33b357de3a84575e8ac27f86d62c93766bfc275076fdc7926e6ccb3"));
            Assert(network.genesis.Header.HashMerkleRoot == uint256.Parse("76417fee12594f59b7a15a7811b562736677557ec68aef76c5c758440017fb49"));

            network.Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                //{ 0, new CheckpointInfo(new uint256("0x00000e246d7b73b88c9ab55f2e5e94d9e22d471def3df5ea448f5576b1d156b9"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                //{ 163000, new CheckpointInfo(new uint256("0x4e44a9e0119a2e7cbf15e570a3c649a5605baa601d953a465b5ebd1c1982212a"), new uint256("0x0646fc7db8f3426eb209e1228c7d82724faa46a060f5bbbd546683ef30be245c")) },
            };

            network.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (65) };
            network.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
            network.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (65 + 128) };
            network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            network.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            network.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            network.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            network.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            network.Base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2a };
            network.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };
            network.Base58Prefixes[(int)Base58Type.COLORED_ADDRESS] = new byte[] { 0x13 };

            var encoder = new Bech32Encoder("bc");
            network.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            network.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            // TODO:Redstone - need seed hosts
            network.DNSSeeds.AddRange(new[]
                {
                    new DNSSeedData("35.176.127.127", "35.176.127.127"), // initial test node will need to be updated
                    //new DNSSeedData("testnet2.stratisplatform.com", "testnet2.stratisplatform.com"),
                    //new DNSSeedData("testnet3.stratisplatform.com", "testnet3.stratisplatform.com"),
                    //new DNSSeedData("testnet4.stratisplatform.com", "testnet4.stratisplatform.com")
                });

            network.SeedNodes.AddRange(new[]
            {
                new NetworkAddress(IPAddress.Parse("35.176.127.127"), network.DefaultPort), // initial test node will need to be updated
                new NetworkAddress(IPAddress.Parse("18.130.33.99"), network.DefaultPort), // danger cloud node
                //new NetworkAddress(IPAddress.Parse("13.70.81.5"), 3389), // beard cloud node  
                //new NetworkAddress(IPAddress.Parse("191.235.85.131"), 3389), // fassa cloud node  
                //new NetworkAddress(IPAddress.Parse("52.232.58.52"), 26178), // neurosploit public node
            });

            Network.Register(network);
            return network;
        }

        private static Network InitRedstoneRegTest()
        {
            var messageStart = new byte[4];
            messageStart[0] = 0xcd;
            messageStart[1] = 0xf2;
            messageStart[2] = 0xc0;
            messageStart[3] = 0xef;
            var magic = BitConverter.ToUInt32(messageStart, 0); // 0xefc0f2cd

            Network network = new Network
            {
                Name = "RedstoneRegTest",
                RootFolderName = RedstoneRootFolderName,
                DefaultConfigFilename = RedstoneDefaultConfigFilename,
                Magic = magic,
                DefaultPort = 19256,
                RPCPort = 19256,
                MaxTimeOffsetSeconds = RedstoneMaxTimeOffsetSeconds,
                MaxTipAge = RedstoneDefaultMaxTipAgeInSeconds
            };

            network.Consensus.SubsidyHalvingInterval = 210000;
            network.Consensus.MajorityEnforceBlockUpgrade = 750;
            network.Consensus.MajorityRejectBlockOutdated = 950;
            network.Consensus.MajorityWindow = 1000;
            network.Consensus.BuriedDeployments[BuriedDeployments.BIP34] = 0;
            network.Consensus.BuriedDeployments[BuriedDeployments.BIP65] = 0;
            network.Consensus.BuriedDeployments[BuriedDeployments.BIP66] = 0;
            network.Consensus.BIP34Hash = new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8");
            network.Consensus.PowLimit = new Target(uint256.Parse("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
            network.Consensus.PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
            network.Consensus.PowTargetSpacing = TimeSpan.FromSeconds(10 * 60);
            network.Consensus.PowAllowMinDifficultyBlocks = true;
            network.Consensus.PowNoRetargeting = true;
            network.Consensus.RuleChangeActivationThreshold = 1916; // 95% of 2016
            network.Consensus.MinerConfirmationWindow = 2016; // nPowTargetTimespan / nPowTargetSpacing
            network.Consensus.LastPOWBlock = 12500;
            network.Consensus.IsProofOfStake = true;
            network.Consensus.ConsensusFactory = new PosConsensusFactory() { Consensus = network.Consensus };
            network.Consensus.ProofOfStakeLimit = new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));
            network.Consensus.ProofOfStakeLimitV2 = new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));
            network.Consensus.CoinType = 105;
            network.Consensus.DefaultAssumeValid = null; // turn off assumevalid for regtest.

            Block genesis = CreateStratisGenesisBlock(network.Consensus.ConsensusFactory, 1470467000, 1831645, 0x1e0fffff, 1, Money.Zero);
            genesis.Header.Time = 1494909211;
            genesis.Header.Nonce = 2433759;
            genesis.Header.Bits = network.Consensus.PowLimit;
            network.genesis = genesis;
            network.Consensus.HashGenesisBlock = genesis.GetHash();
            Assert(network.Consensus.HashGenesisBlock == uint256.Parse("0x93925104d664314f581bc7ecb7b4bad07bcfabd1cfce4256dbd2faddcf53bd1f"));

            network.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (65) };
            network.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
            network.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (65 + 128) };
            network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            network.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            network.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            network.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            network.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            network.Base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2a };
            network.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };
            network.Base58Prefixes[(int)Base58Type.COLORED_ADDRESS] = new byte[] { 0x13 };

            var encoder = new Bech32Encoder("bc");
            network.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            network.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            Network.Register(network);

            return network;
        }


        private static Block CreateRedstoneGenesisBlock(ConsensusFactory consensusFactory, uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
            string pszTimestamp = "http://www.bbc.co.uk/sport/football/43632830";
            return CreateRedstoneGenesisBlock(consensusFactory, pszTimestamp, nTime, nNonce, nBits, nVersion, genesisReward);
        }

        private static Block CreateRedstoneGenesisBlock(ConsensusFactory consensusFactory, string pszTimestamp, uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
            Transaction txNew = consensusFactory.CreateTransaction();
            txNew.Version = 1;
            txNew.Time = nTime;
            txNew.AddInput(new TxIn()
            {
                ScriptSig = new Script(Op.GetPushOp(0), new Op()
                {
                    Code = (OpcodeType)0x1,
                    PushData = new[] { (byte)42 }
                }, Op.GetPushOp(Encoders.ASCII.DecodeData(pszTimestamp)))
            });
            txNew.AddOutput(new TxOut()
            {
                Value = genesisReward,
            });
            Block genesis = consensusFactory.CreateBlock();
            genesis.Header.BlockTime = Utils.UnixTimeToDateTime(nTime);
            genesis.Header.Bits = nBits;
            genesis.Header.Nonce = nNonce;
            genesis.Header.Version = nVersion;
            genesis.Transactions.Add(txNew);
            genesis.Header.HashPrevBlock = uint256.Zero;
            genesis.UpdateMerkleRoot();
            return genesis;
        }
    }
}
