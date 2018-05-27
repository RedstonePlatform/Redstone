using System;
using System.Collections.Generic;
using System.Net;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Stratis.Bitcoin.Utilities;

namespace Redstone.Core
{
    public class RedstoneNetwork
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
            Block.BlockSignature = true;
            Transaction.TimeStamp = true;

            var consensus = new Consensus();

            // TODO:Redstone - nned to decide on coin defaults
            consensus.NetworkOptions = new NetworkOptions() { IsProofOfStake = true };
            consensus.GetPoWHash = (n, h) => NBitcoin.Crypto.HashX13.Instance.Hash(h.ToBytes(options: n));

            consensus.SubsidyHalvingInterval = 210000;
            consensus.MajorityEnforceBlockUpgrade = 750;
            consensus.MajorityRejectBlockOutdated = 950;
            consensus.MajorityWindow = 1000;
            consensus.BuriedDeployments[BuriedDeployments.BIP34] = 227931;
            consensus.BuriedDeployments[BuriedDeployments.BIP65] = 388381;
            consensus.BuriedDeployments[BuriedDeployments.BIP66] = 363725;
            consensus.BIP34Hash = new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8");
            consensus.PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
            consensus.PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
            consensus.PowTargetSpacing = TimeSpan.FromSeconds(10 * 60);
            consensus.PowAllowMinDifficultyBlocks = false;
            consensus.PowNoRetargeting = false;
            consensus.RuleChangeActivationThreshold = 1916; // 95% of 2016
            consensus.MinerConfirmationWindow = 2016; // nPowTargetTimespan / nPowTargetSpacing

            consensus.BIP9Deployments[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, 1199145601, 1230767999);
            consensus.BIP9Deployments[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, 1462060800, 1493596800);
            consensus.BIP9Deployments[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, 0, 0);

            consensus.LastPOWBlock = 12500;

            consensus.ProofOfStakeLimit = new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));
            consensus.ProofOfStakeLimitV2 = new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));

            consensus.CoinType = 1001;

            consensus.DefaultAssumeValid = new uint256("0x8c2cf95f9ca72e13c8c4cdf15c2d7cc49993946fb49be4be147e106d502f1869"); // 642930

            Block genesis = CreateRedstoneGenesisBlock(1470467000, 1831645, 0x1e0fffff, 1, Money.Zero);
            consensus.HashGenesisBlock = genesis.GetHash(consensus.NetworkOptions);

            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0x70;
            messageStart[1] = 0x35;
            messageStart[2] = 0x22;
            messageStart[3] = 0x05;
            var magic = BitConverter.ToUInt32(messageStart, 0); //0x5223570; 

            // TODO:Redstone - new genesisblock and merkleroot           
            Guard.Assert(consensus.HashGenesisBlock == uint256.Parse("f0df827e7b388fc76cfe479abf10b7ee2769a16a94d0d1c3b6432d74a796f1e3"));
            Guard.Assert(genesis.Header.HashMerkleRoot == uint256.Parse("76417fee12594f59b7a15a7811b562736677557ec68aef76c5c758440017fb49"));

            var builder = new NetworkBuilder()
                .SetName("RedstoneMain")
                .SetRootFolderName(RedstoneRootFolderName)
                .SetDefaultConfigFilename(RedstoneDefaultConfigFilename)
                .SetConsensus(consensus)
                .SetMagic(magic)
                .SetGenesis(genesis)
                .SetPort(19056) // Redstone: UPDATED
                .SetRPCPort(19057) // Redstone: UPDATED
                .SetTxFees(10000, 60000, 10000)
                .SetMaxTimeOffsetSeconds(RedstoneMaxTimeOffsetSeconds)
                .SetMaxTipAge(RedstoneDefaultMaxTipAgeInSeconds)

                // TODO:Redstone - need seed hosts
                .AddDNSSeeds(new[]
                {
                    new DNSSeedData("35.176.127.127", "35.176.127.127"), // initial test node will need to be updated
                    //new DNSSeedData("testnet2.stratisplatform.com", "testnet2.stratisplatform.com"),
                    //new DNSSeedData("testnet3.stratisplatform.com", "testnet3.stratisplatform.com"),
                    //new DNSSeedData("testnet4.stratisplatform.com", "testnet4.stratisplatform.com")
                })

                // TODO:Redstone - what are these - documented for bitcoin here: https://dev.visucore.com/bitcoin/doxygen/class_c_chain_params.html
                .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { (63) })
                .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { (125) })
                .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { (63 + 128) })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] { 0x01, 0x42 })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] { 0x01, 0x43 })
                .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { (0x04), (0x88), (0xB2), (0x1E) })
                .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { (0x04), (0x88), (0xAD), (0xE4) })
                .SetBase58Bytes(Base58Type.PASSPHRASE_CODE, new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 })
                .SetBase58Bytes(Base58Type.CONFIRMATION_CODE, new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A })
                .SetBase58Bytes(Base58Type.STEALTH_ADDRESS, new byte[] { 0x2a })
                .SetBase58Bytes(Base58Type.ASSET_ID, new byte[] { 23 })
                .SetBase58Bytes(Base58Type.COLORED_ADDRESS, new byte[] { 0x13 })
                .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, "bc")
                .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, "bc");

            // TODO:Redstone - update
            var seed = new[] { "101.200.198.155", "103.24.76.21", "104.172.24.79" };
            var fixedSeeds = new List<NetworkAddress>();
            // Convert the pnSeeds array into usable address objects.
            Random rand = new Random();
            TimeSpan oneWeek = TimeSpan.FromDays(7);
            for (int i = 0; i < seed.Length; i++)
            {
                // It'll only connect to one or two seed nodes because once it connects,
                // it'll get a pile of addresses with newer timestamps.                
                NetworkAddress addr = new NetworkAddress();
                // Seed nodes are given a random 'last seen time' of between one and two
                // weeks ago.
                addr.Time = DateTime.UtcNow - (TimeSpan.FromSeconds(rand.NextDouble() * oneWeek.TotalSeconds)) - oneWeek;
                addr.Endpoint = Utils.ParseIpEndpoint(seed[i], builder.Port);
                fixedSeeds.Add(addr);
            }

            builder.AddSeeds(fixedSeeds);
            return builder.BuildAndRegister();
        }

        private static Network InitRedstoneTest()
        {
            Block.BlockSignature = true;
            Transaction.TimeStamp = true;

            var consensus = RedstoneMain.Consensus.Clone();
            consensus.PowLimit = new Target(uint256.Parse("0000ffff00000000000000000000000000000000000000000000000000000000"));

            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0x71;
            messageStart[1] = 0x31;
            messageStart[2] = 0x21;
            messageStart[3] = 0x11;
            var magic = BitConverter.ToUInt32(messageStart, 0); //0x5223570; 

            var genesis = RedstoneMain.GetGenesis();
            genesis.Header.Time = 1493909211;
            genesis.Header.Nonce = 2433759;
            genesis.Header.Bits = consensus.PowLimit;
            consensus.HashGenesisBlock = genesis.GetHash(consensus.NetworkOptions);

            Guard.Assert(consensus.HashGenesisBlock == uint256.Parse("5166f378d33b357de3a84575e8ac27f86d62c93766bfc275076fdc7926e6ccb3"));

            consensus.DefaultAssumeValid = new uint256("0x12ae16993ce7f0836678f225b2f4b38154fa923bd1888f7490051ddaf4e9b7fa"); // 218810

            var builder = new NetworkBuilder()
                .SetName("RedstoneTest")
                .SetRootFolderName(RedstoneRootFolderName)
                .SetDefaultConfigFilename(RedstoneDefaultConfigFilename)
                .SetConsensus(consensus)
                .SetMagic(magic)
                .SetGenesis(genesis)
                .SetPort(19156)
                .SetRPCPort(19157)
                .SetMaxTimeOffsetSeconds(RedstoneMaxTimeOffsetSeconds)
                .SetMaxTipAge(RedstoneDefaultMaxTipAgeInSeconds)
                .SetTxFees(10000, 60000, 10000)
                .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { (65) })
                .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { (196) })
                .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { (65 + 128) })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] { 0x01, 0x42 })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] { 0x01, 0x43 })
                .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { (0x04), (0x88), (0xB2), (0x1E) })
                .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { (0x04), (0x88), (0xAD), (0xE4) })

                // TODO:Redstone - need seed hosts
                .AddDNSSeeds(new[]
                {
                    new DNSSeedData("35.176.127.127", "35.176.127.127"), // initial test node will need to be updated
                    //new DNSSeedData("testnet2.stratisplatform.com", "testnet2.stratisplatform.com"),
                    //new DNSSeedData("testnet3.stratisplatform.com", "testnet3.stratisplatform.com"),
                    //new DNSSeedData("testnet4.stratisplatform.com", "testnet4.stratisplatform.com")
                });

            builder.AddSeeds(new[]
            {
                new NetworkAddress(IPAddress.Parse("35.176.127.127"), builder.Port), // initial test node will need to be updated
                new NetworkAddress(IPAddress.Parse("18.130.33.99"), builder.Port), // danger cloud node
                //new NetworkAddress(IPAddress.Parse("13.70.81.5"), 3389), // beard cloud node  
                //new NetworkAddress(IPAddress.Parse("191.235.85.131"), 3389), // fassa cloud node  
                //new NetworkAddress(IPAddress.Parse("52.232.58.52"), 26178), // neurosploit public node
            });

            return builder.BuildAndRegister();
        }

        private static Network InitRedstoneRegTest()
        {
            // TODO: move this to Networks
            var net = Network.GetNetwork("RedstoneRegTest");
            if (net != null)
                return net;

            Block.BlockSignature = true;
            Transaction.TimeStamp = true;

            var consensus = RedstoneTest.Consensus.Clone();
            consensus.PowLimit = new Target(uint256.Parse("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));

            consensus.PowAllowMinDifficultyBlocks = true;
            consensus.PowNoRetargeting = true;

            var messageStart = new byte[4];
            messageStart[0] = 0xcd;
            messageStart[1] = 0xf2;
            messageStart[2] = 0xc0;
            messageStart[3] = 0xef;
            var magic = BitConverter.ToUInt32(messageStart, 0);

            var genesis = RedstoneMain.GetGenesis();
            genesis.Header.Time = 1494909211;
            genesis.Header.Nonce = 2433759;
            genesis.Header.Bits = consensus.PowLimit;
            consensus.HashGenesisBlock = genesis.GetHash(consensus.NetworkOptions);

            Guard.Assert(consensus.HashGenesisBlock == uint256.Parse("0x93925104d664314f581bc7ecb7b4bad07bcfabd1cfce4256dbd2faddcf53bd1f"));

            consensus.DefaultAssumeValid = null; // turn off assumevalid for regtest.

            var builder = new NetworkBuilder()
                .SetName("RedstoneRegTest")
                .SetRootFolderName(RedstoneRootFolderName)
                .SetDefaultConfigFilename(RedstoneDefaultConfigFilename)
                .SetConsensus(consensus)
                .SetMagic(magic)
                .SetGenesis(genesis)
                .SetPort(19256)
                .SetRPCPort(19257)
                .SetMaxTimeOffsetSeconds(RedstoneMaxTimeOffsetSeconds)
                .SetMaxTipAge(RedstoneDefaultMaxTipAgeInSeconds)
                .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { (65) })
                .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { (196) })
                .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { (65 + 128) })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] { 0x01, 0x42 })
                .SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] { 0x01, 0x43 })
                .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { (0x04), (0x88), (0xB2), (0x1E) })
                .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { (0x04), (0x88), (0xAD), (0xE4) });

            return builder.BuildAndRegister();
        }
        
        
        private static Block CreateRedstoneGenesisBlock(uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
            string pszTimestamp = "http://www.bbc.co.uk/sport/football/43632830";
            return CreateRedstoneGenesisBlock(pszTimestamp, nTime, nNonce, nBits, nVersion, genesisReward);
        }

        private static Block CreateRedstoneGenesisBlock(string pszTimestamp, uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
            Transaction txNew = new Transaction();
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
            Block genesis = new Block();
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
