using System;
using System.Net;
using System.Collections.Generic;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.Protocol;
using Stratis.Bitcoin.Features.Wallet;
using Redstone.Core.Networks.Deployments;

namespace Redstone.Core.Networks
{
    public class RedstoneMain : RedstoneBaseNetwork
    {
        public RedstoneMain()
        {
            this.SetDefaults();

            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0xb9;
            messageStart[1] = 0xd0;
            messageStart[2] = 0xae;
            messageStart[3] = 0xd7;
            uint magic = BitConverter.ToUInt32(messageStart, 0); // 0xd7aed0b9 = ×®Ð¹

            this.Name = "RedstoneMain";
            this.NetworkType = NetworkType.Mainnet;
            this.Magic = magic;
            this.DefaultPort = 19056;
            this.DefaultRPCPort = 19057;
            this.DefaultAPIPort = 37222;
            this.CoinTicker = "XRD";

            var consensusFactory = new PosConsensusFactory();

            // Create the genesis block.
            this.GenesisTime = 1550609400; // Tuesday, 19 February 2019 20:50:00 was 1470467000;
            this.GenesisNonce = 1831645;
            this.GenesisBits = 0x1e0fffff;

            CreateRedstoneGenesisBlock(consensusFactory);

            // Taken from StratisX.
            var consensusOptions = new PosConsensusOptions(
                maxBlockBaseSize: 1_000_000,
                maxStandardVersion: 2,
                maxStandardTxWeight: 100_000,
                maxBlockSigopsCost: 20_000,
                maxStandardTxSigopsCost: 20_000 / 5
            );

            var buriedDeployments = new BuriedDeploymentsArray
            {
                [BuriedDeployments.BIP34] = 0,
                [BuriedDeployments.BIP65] = 0,
                [BuriedDeployments.BIP66] = 0
            };

            var bip9Deployments = new RedstoneBIP9Deployments();

            this.Consensus = new Consensus(
                consensusFactory: consensusFactory,
                consensusOptions: consensusOptions,
                coinType: (int)CoinType.Redstone,
                hashGenesisBlock: this.Genesis.GetHash(),
                subsidyHalvingInterval: 210000,
                majorityEnforceBlockUpgrade: 750,
                majorityRejectBlockOutdated: 950,
                majorityWindow: 1000,
                buriedDeployments: buriedDeployments,
                bip9Deployments: bip9Deployments,
                bip34Hash: new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
                ruleChangeActivationThreshold: 1916, // 95% of 2016
                minerConfirmationWindow: 2016, // nPowTargetTimespan / nPowTargetSpacing
                maxReorgLength: 500,
                defaultAssumeValid: null,
                maxMoney: long.MaxValue,
                coinbaseMaturity: 50,
                premineHeight: 2,
                premineReward: Money.Coins(5_400_000),
                proofOfWorkReward: Money.Coins(30),
                powTargetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
                powTargetSpacing: TimeSpan.FromSeconds(10 * 60),
                powAllowMinDifficultyBlocks: false,
                posNoRetargeting: false,
                powNoRetargeting: false,
                powLimit: new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                minimumChainWork: null,
                isProofOfStake: true,
                lastPowBlock: 129600,
                proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeReward: Money.Coins(15),
                posRewardReduction: true,
                posRewardReductionBlockInterval: 525600,
                posRewardReductionPercentage: 7.5m,
                posRewardMinter: .45m,
                posRewardServiceNode: .45m,
                posRewardFoundation: .1m,
                serviceNodeCollateralThreshold: 1500,
                serviceNodeCollateralBlockPeriod: 30
            );

            this.SetBase58Prefixes(new byte[] { (60) }, new byte[] { (122) }, new byte[] { (60 + 128) });

            this.Checkpoints = new Dictionary<int, CheckpointInfo>
            {
            };

            this.DNSSeeds = new List<DNSSeedData>()
            {
                new DNSSeedData("seed.redstonecoin.com", "seed.redstonecoin.com"),
            };

            this.SeedNodes = new List<NetworkAddress>
            {
               new NetworkAddress(IPAddress.Parse("80.211.84.170"), this.DefaultPort), // cryptohunter node #4
               new NetworkAddress(IPAddress.Parse("31.14.138.23"), this.DefaultPort), // cryptohunter node #3
               new NetworkAddress(IPAddress.Parse("35.204.238.255"), this.DefaultPort), // cryptohunter node #googlecloud
            };

            Assert(this.Consensus.HashGenesisBlock == uint256.Parse("8e21759b1aefe10358fef84da1ac428af6ba17990b7eee71c47de9582fa31806"));
            Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse("c89473b52c9a1afbc3784b0306fd06e86d016c13d68b56343c78a9377491a2f7"));
        }
    }
}
