using Redstone.Core.Policies;

namespace Redstone.Core.Networks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using NBitcoin;
    using NBitcoin.BouncyCastle.Math;
    using NBitcoin.Protocol;
    using Stratis.Bitcoin.Features.Wallet;
    using Redstone.Core.Networks.Deployments;
    using NBitcoin.DataEncoders;

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
            uint magic = BitConverter.ToUInt32(messageStart, 0); // 0x11233171 TODO: d7aed0b2 = ×®Ð²

            this.Name = "RedstoneTest";
            this.Magic = magic;
            this.DefaultPort = 19156;
            this.RPCPort = 19157;
            this.CoinTicker = "TXRD";
            this.MaxTipAge = RedstoneDefaultMaxTipAgeInSeconds * 12 * 365;

            var powLimit = new Target(new uint256("0000ffff00000000000000000000000000000000000000000000000000000000"));

            var consensusFactory = new PosConsensusFactory();

            // Create the genesis block.
            this.GenesisTime = 1530256857;
            this.GenesisNonce = 1349369;
            this.GenesisBits = this.Consensus.PowLimit;
            this.GenesisVersion = 1;
            this.GenesisReward = Money.Zero;

            Block genesisBlock = CreateRedstoneGenesisBlock(consensusFactory, this.GenesisTime, this.GenesisNonce, this.GenesisBits, this.GenesisVersion, this.GenesisReward);

            genesisBlock.Header.Time = 1544474470;
            genesisBlock.Header.Nonce = 2433759;
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
                coinType: (int)CoinType.Redstone,
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
                defaultAssumeValid: new uint256("0x98fa6ef0bca5b431f15fd79dc6f879dc45b83ed4b1bbe933a383ef438321958e"), // 372652
                maxMoney: long.MaxValue,
                coinbaseMaturity: 10,
                premineHeight: 2,
                premineReward: Money.Coins(30000),
                proofOfWorkReward: Money.Coins(30),
                powTargetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
                powTargetSpacing: TimeSpan.FromSeconds(10 * 60),
                powAllowMinDifficultyBlocks: false,
                powNoRetargeting: false,
                powLimit: powLimit,
                minimumChainWork: null,
                isProofOfStake: true,
                lastPowBlock: 1440,
                proofOfStakeLimit: new BigInteger(uint256.Parse("0000ffff00000000000000000000000000000000000000000000000000000000").ToBytes(false)),
                proofOfStakeLimitV2: new BigInteger(uint256.Parse("0000ffff00000000000000000000000000000000000000000000000000000000").ToBytes(false)),
                proofOfStakeReward: Money.Coins(15),
                posRewardReduction: true,
                posRewardReductionBlockInterval: 2880,
                posRewardReductionPercentage: 7.5m
            );

            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (65) };
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (65 + 128) };

            this.Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // { 0, new CheckpointInfo(new uint256("0x5166f378d33b357de3a84575e8ac27f86d62c93766bfc275076fdc7926e6ccb3"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                // { 2, new CheckpointInfo(new uint256("0xff24fef45f00088ef09b713d24adc07494bedf69d93645600b76debbd38cbedf"), new uint256("0x7d61c139a471821caa6b7635a4636e90afcfe5e195040aecbc1ad7d24924db1e")) }, // Premine
                // { 261, new CheckpointInfo(new uint256("0xfde037496468d67c1e0b76656ccfc90d2a4b8b489c7b05599de7ae58d85c10f2"), new uint256("0x7d61c139a471821caa6b7635a4636e90afcfe5e195040aecbc1ad7d24924db1e")) },
            };

            this.DNSSeeds = new List<DNSSeedData>()
            {
                new DNSSeedData("seed.redstoneplatform.com", "seed.redstoneplatform.com")
            };

            this.SeedNodes = new List<NetworkAddress>
            {
               new NetworkAddress(IPAddress.Parse("80.211.88.201"), this.DefaultPort), // cryptohunter node #8
               new NetworkAddress(IPAddress.Parse("80.211.88.233"), this.DefaultPort), // cryptohunter node #9
               new NetworkAddress(IPAddress.Parse("80.211.88.244"), this.DefaultPort), // cryptohunter node #10
               new NetworkAddress(IPAddress.Parse("80.211.84.170"), this.DefaultPort), // cryptohunter node #4
            };

            this.StandardScriptsRegistry = new RedstoneStandardScriptsRegistry();

            Assert(this.Consensus.HashGenesisBlock == uint256.Parse("5b3bce1db145b398f502782d4fbef62cbb46205a41bb4aa37cda3619729e3037"));
        }
    }
}
