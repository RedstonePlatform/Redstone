using Redstone.Core.Networks.Deployments;
using Redstone.Core.Policies;
using Stratis.Bitcoin.Features.Wallet;

namespace Redstone.Core.Networks
{
    using System;
    using System.Collections.Generic;
    using NBitcoin;
    using NBitcoin.BouncyCastle.Math;
    using NBitcoin.Protocol;

    public class RedstoneRegTest : RedstoneMain
    {
        public RedstoneRegTest()
        {
            var messageStart = new byte[4];
            messageStart[0] = 0xb3;
            messageStart[1] = 0xd0;
            messageStart[2] = 0xae;
            messageStart[3] = 0xd7;
            uint magic = BitConverter.ToUInt32(messageStart, 0); // 0xd7aed0b3 = ×®Ð³

            this.Name = "RedstoneRegTest";
            this.NetworkType = NetworkType.Testnet;
            this.Magic = magic;
            this.DefaultAPIPort = 19256;
            this.DefaultRPCPort = 19257;
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

            Block genesisBlock = CreateRedstoneGenesisBlock(consensusFactory, this.GenesisTime, this.GenesisNonce, this.GenesisBits, this.GenesisVersion, this.GenesisReward);

            genesisBlock.Header.Time = 1531811966;
            genesisBlock.Header.Nonce = 1349369;
            genesisBlock.Header.Bits = powLimit;

            this.Genesis = genesisBlock;

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
                coinType: (int)CoinType.Redstone, // unique coin type TODO how do we get this added
                hashGenesisBlock: genesisBlock.GetHash(),
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
                defaultAssumeValid: null, // turn off assumevalid for regtest.
                maxMoney: long.MaxValue,
                coinbaseMaturity: 10,
                premineHeight: 2,
                premineReward: Money.Coins(30000),
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
                posRewardReductionPercentage: 7.5m
            );

            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (63) };
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (63 + 128) };

            this.Checkpoints = new Dictionary<int, CheckpointInfo>();
            this.DNSSeeds = new List<DNSSeedData>();
            this.SeedNodes = new List<NetworkAddress>();

            this.StandardScriptsRegistry = new RedstoneStandardScriptsRegistry();

            Assert(this.Consensus.HashGenesisBlock == uint256.Parse("4442244290302b76a11951ba648e40bcdc6cc3965a99c30018ade95a4bc6e6bd"));
        }
    }
}