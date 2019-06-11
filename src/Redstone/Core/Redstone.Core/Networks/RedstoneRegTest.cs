using System;
using System.Collections.Generic;
using NBitcoin.Protocol;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using Redstone.Core.Deployments;
using Stratis.Bitcoin.Features.Wallet;

namespace Redstone.Core.Networks
{
    public class RedstoneRegTest : RedstoneBaseNetwork
    {
        public RedstoneRegTest()
        {
            this.SetDefaults();

            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0xb3;
            messageStart[1] = 0xd0;
            messageStart[2] = 0xae;
            messageStart[3] = 0xd7;
            uint magic = BitConverter.ToUInt32(messageStart, 0); // 0xd7aed0b3 = ×®Ð³

            this.Name = "RedstoneRegTest";
            this.NetworkType = NetworkType.Regtest;
            this.Magic = magic;
            this.DefaultPort = 19256;
            this.DefaultRPCPort = 19257;
            this.DefaultAPIPort = 39222;
            this.MinTxFee = 0;
            this.FallbackFee = 0;
            this.MinRelayTxFee = 0;
            this.CoinTicker = "TXRD";

            var powLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));

            var consensusFactory = new PosConsensusFactory();

            // Create the genesis block.
            this.GenesisTime = 1470467000;
            this.GenesisNonce = 1831645;
            this.GenesisBits = 0x1e0fffff;
            this.GenesisVersion = 1;
            this.GenesisReward = Money.Zero;

            this.CreateRedstoneGenesisBlock(consensusFactory);

            this.Genesis.Header.Time = 1494909211;
            this.Genesis.Header.Nonce = 2433759;
            this.Genesis.Header.Bits = powLimit;

            var bip9Deployments = new RedstoneBIP9Deployments()
            {
                // Always active on StratisRegTest.
                [RedstoneBIP9Deployments.ColdStaking] = new BIP9DeploymentsParameters(1, BIP9DeploymentsParameters.AlwaysActive, 999999999)
            };

            this.Consensus = new Consensus(
                consensusFactory: consensusFactory,
                consensusOptions: PosConsensusOptions,
                coinType: (int)CoinType.Redstone, // unique coin type TODO how do we get this added
                hashGenesisBlock: this.Genesis.GetHash(),
                subsidyHalvingInterval: 210000,
                majorityEnforceBlockUpgrade: 750,
                majorityRejectBlockOutdated: 950,
                majorityWindow: 1000,
                buriedDeployments: BuriedDeployments,
                bip9Deployments: bip9Deployments,
                bip34Hash: new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
                ruleChangeActivationThreshold: 1916, // 95% of 2016
                minerConfirmationWindow: 2016, // nPowTargetTimespan / nPowTargetSpacing
                maxReorgLength: 500,
                defaultAssumeValid: null, // turn off assumevalid for regtest.
                maxMoney: long.MaxValue,
                coinbaseMaturity: 10,
                premineHeight: 2,
                premineReward: Money.Coins(98000000),
                proofOfWorkReward: Money.Coins(30),
                powTargetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
                powTargetSpacing: TimeSpan.FromSeconds(10 * 60),
                powAllowMinDifficultyBlocks: true,
                posNoRetargeting: true,
                powNoRetargeting: true,
                powLimit: powLimit,
                minimumChainWork: null,
                isProofOfStake: true,
                lastPowBlock: 1440,
                proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeReward: Money.Coins(15),
                posRewardReduction: true,
                posRewardReductionBlockInterval: 2880,
                posRewardReductionPercentage: 7.5m,
                posRewardMinterPercentage: .45m,
                posRewardServiceNodePercentage: .45m,
                posRewardFoundationPercentage: .1m,
                posRewardFoundationPubKeyHash: "1c271555001998be1ef9718a786f21ea500a1c2e",
                serviceNodeCollateralThreshold: 100,
                serviceNodeCollateralBlockPeriod: 5
            );

            this.SetBase58Prefixes(new byte[] { (63) }, new byte[] { (196) }, new byte[] { (63 + 128) });

            this.Checkpoints = new Dictionary<int, CheckpointInfo>();
            this.DNSSeeds = new List<DNSSeedData>();
            this.SeedNodes = new List<NetworkAddress>();

            //Assert(this.Consensus.HashGenesisBlock == uint256.Parse("73adc2f9728610254f81586493df43fd9f0b97b933c6dd1795c53cf52e5d4739"));
        }
    }
}