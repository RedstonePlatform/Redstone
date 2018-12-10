namespace Redstone.Core.Tests
{
    using System;
    using NBitcoin;
    using NBitcoin.BouncyCastle.Math;
    using NBitcoin.DataEncoders;
    using NBitcoin.Networks;
    using Networks;
    using Redstone.Core.Networks.Deployments;
    using Xunit;

    public class NetworkTests
    {
        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void CanGetNetworkFromName()
        {
            Network redstoneMain = RedstoneNetworks.Main;
            Network redstoneTest = RedstoneNetworks.TestNet;
            Network redstoneRegtest = RedstoneNetworks.RegTest;

            Assert.Equal(NetworkRegistration.GetNetwork("redstoneMain"), redstoneMain);
            Assert.Equal(NetworkRegistration.GetNetwork("RedstoneMain"), redstoneMain);
            Assert.Equal(NetworkRegistration.GetNetwork("RedstoneTest"), redstoneTest);
            Assert.Equal(NetworkRegistration.GetNetwork("RedstoneTest"), redstoneTest);
            Assert.Equal(NetworkRegistration.GetNetwork("redstoneRegtest"), redstoneRegtest);
            Assert.Equal(NetworkRegistration.GetNetwork("RedstoneRegtest"), redstoneRegtest);
            Assert.Null(NetworkRegistration.GetNetwork("invalid"));
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void RedstoneMainIsInitializedCorrectly()
        {
            Network network = RedstoneNetworks.Main;

            Assert.Empty(network.Checkpoints);
            Assert.Empty(network.DNSSeeds);
            Assert.Empty(network.SeedNodes);

            Assert.Equal("RedstoneMain", network.Name);
            Assert.Equal(RedstoneMain.RedstoneRootFolderName, network.RootFolderName);
            Assert.Equal(RedstoneMain.RedstoneDefaultConfigFilename, network.DefaultConfigFilename);
            Assert.Equal(0x5223570.ToString(), network.Magic.ToString());
            Assert.Equal(19056, network.DefaultPort);
            Assert.Equal(19057, network.RPCPort);
            Assert.Equal(RedstoneMain.RedstoneMaxTimeOffsetSeconds, network.MaxTimeOffsetSeconds);
            Assert.Equal(RedstoneMain.RedstoneDefaultMaxTipAgeInSeconds, network.MaxTipAge);
            Assert.Equal(10000, network.MinTxFee);
            Assert.Equal(60000, network.FallbackFee);
            Assert.Equal(10000, network.MinRelayTxFee);
            Assert.Equal("XRD", network.CoinTicker);

            Assert.Equal(2, network.Bech32Encoders.Length);
            Assert.Equal(new Bech32Encoder("bc").ToString(),
                network.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS].ToString());
            Assert.Equal(new Bech32Encoder("bc").ToString(),
                network.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS].ToString());

            Assert.Equal(12, network.Base58Prefixes.Length);
            Assert.Equal(new byte[] { (60) }, network.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS]);
            Assert.Equal(new byte[] { (122) }, network.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS]);
            Assert.Equal(new byte[] { (60 + 128) }, network.Base58Prefixes[(int)Base58Type.SECRET_KEY]);
            Assert.Equal(new byte[] { 0x01, 0x42 }, network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC]);
            Assert.Equal(new byte[] { 0x01, 0x43 }, network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC]);
            Assert.Equal(new byte[] { (0x04), (0x88), (0xB2), (0x1E) },
                network.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY]);
            Assert.Equal(new byte[] { (0x04), (0x88), (0xAD), (0xE4) },
                network.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY]);
            Assert.Equal(new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 },
                network.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE]);
            Assert.Equal(new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A },
                network.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE]);
            Assert.Equal(new byte[] { 0x2a }, network.Base58Prefixes[(int)Base58Type.STEALTH_ADDRESS]);
            Assert.Equal(new byte[] { 23 }, network.Base58Prefixes[(int)Base58Type.ASSET_ID]);
            Assert.Equal(new byte[] { 0x13 }, network.Base58Prefixes[(int)Base58Type.COLORED_ADDRESS]);

            Assert.Equal(210000, network.Consensus.SubsidyHalvingInterval);
            Assert.Equal(750, network.Consensus.MajorityEnforceBlockUpgrade);
            Assert.Equal(950, network.Consensus.MajorityRejectBlockOutdated);
            Assert.Equal(1000, network.Consensus.MajorityWindow);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP34]);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP65]);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP66]);
            Assert.Equal(new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
                network.Consensus.BIP34Hash);
            Assert.Equal(new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                network.Consensus.PowLimit);
            Assert.Null(network.Consensus.MinimumChainWork);
            Assert.Equal(TimeSpan.FromSeconds(14 * 24 * 60 * 60), network.Consensus.PowTargetTimespan);
            Assert.Equal(TimeSpan.FromSeconds(10 * 60), network.Consensus.PowTargetSpacing);
            Assert.False(network.Consensus.PowAllowMinDifficultyBlocks);
            Assert.False(network.Consensus.PowNoRetargeting);
            Assert.Equal(1916, network.Consensus.RuleChangeActivationThreshold);
            Assert.Equal(2016, network.Consensus.MinerConfirmationWindow);
            Assert.Null(network.Consensus.BIP9Deployments[RedstoneBIP9Deployments.TestDummy]);
            Assert.Equal(129600, network.Consensus.LastPOWBlock);
            Assert.True(network.Consensus.IsProofOfStake);
            Assert.Equal(787264, network.Consensus.CoinType);
            Assert.Equal(
                new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")
                    .ToBytes(false)), network.Consensus.ProofOfStakeLimit);
            Assert.Equal(
                new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff")
                    .ToBytes(false)), network.Consensus.ProofOfStakeLimitV2);
            Assert.Equal(new uint256("0x55a8205ae4bbf18f4d238c43f43005bd66e0b1f679b39e2c5c62cf6903693a5e"),
                network.Consensus.DefaultAssumeValid);
            Assert.Equal(50, network.Consensus.CoinbaseMaturity);
            Assert.Equal(Money.Coins(5400000), network.Consensus.PremineReward);
            Assert.Equal(2, network.Consensus.PremineHeight);
            Assert.Equal(Money.Coins(30), network.Consensus.ProofOfWorkReward);
            Assert.Equal(Money.Coins(15), network.Consensus.ProofOfStakeReward);
            Assert.True(network.Consensus.PosRewardReduction);
            Assert.Equal(525_600, network.Consensus.PosRewardReductionBlockInterval);
            Assert.Equal(7.5m, network.Consensus.PosRewardReductionPercentage);
            Assert.Equal((uint)500, network.Consensus.MaxReorgLength);
            Assert.Equal(long.MaxValue, network.Consensus.MaxMoney);

            Block genesis = network.GetGenesis();
            Assert.Equal(uint256.Parse("0xc25b823deb69fdc740c3142dfdfb155274655905e2120e82ebf88818a552a5a0"),
                genesis.GetHash());
            Assert.Equal(uint256.Parse("0x0ad80b454d4060b0f9bc821d94ec14da59ea7194a9fd3875b31c14873d202b7d"),
                genesis.Header.HashMerkleRoot);
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void RedstoneTestnetIsInitializedCorrectly()
        {
            Network network = RedstoneNetworks.TestNet;

            Assert.Empty(network.Checkpoints);
            Assert.Empty(network.DNSSeeds);
            Assert.Empty(network.SeedNodes);

            Assert.Equal("RedstoneTest", network.Name);
            Assert.Equal(RedstoneMain.RedstoneRootFolderName, network.RootFolderName);
            Assert.Equal(RedstoneMain.RedstoneDefaultConfigFilename, network.DefaultConfigFilename);
            Assert.Equal(0x11233171.ToString(), network.Magic.ToString());
            Assert.Equal(19156, network.DefaultPort);
            Assert.Equal(19157, network.RPCPort);
            Assert.Equal(RedstoneMain.RedstoneMaxTimeOffsetSeconds, network.MaxTimeOffsetSeconds);
            Assert.Equal(RedstoneMain.RedstoneDefaultMaxTipAgeInSeconds, network.MaxTipAge);
            Assert.Equal(10000, network.MinTxFee);
            Assert.Equal(60000, network.FallbackFee);
            Assert.Equal(10000, network.MinRelayTxFee);
            Assert.Equal("TXRD", network.CoinTicker);

            Assert.Equal(2, network.Bech32Encoders.Length);
            Assert.Equal(new Bech32Encoder("bc").ToString(),
                network.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS].ToString());
            Assert.Equal(new Bech32Encoder("bc").ToString(),
                network.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS].ToString());

            Assert.Equal(12, network.Base58Prefixes.Length);
            Assert.Equal(new byte[] { (63) }, network.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS]);
            Assert.Equal(new byte[] { (196) }, network.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS]);
            Assert.Equal(new byte[] { (63 + 128) }, network.Base58Prefixes[(int)Base58Type.SECRET_KEY]);
            Assert.Equal(new byte[] { 0x01, 0x42 }, network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC]);
            Assert.Equal(new byte[] { 0x01, 0x43 }, network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC]);
            Assert.Equal(new byte[] { (0x04), (0x88), (0xB2), (0x1E) },
                network.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY]);
            Assert.Equal(new byte[] { (0x04), (0x88), (0xAD), (0xE4) },
                network.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY]);
            Assert.Equal(new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 },
                network.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE]);
            Assert.Equal(new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A },
                network.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE]);
            Assert.Equal(new byte[] { 0x2a }, network.Base58Prefixes[(int)Base58Type.STEALTH_ADDRESS]);
            Assert.Equal(new byte[] { 23 }, network.Base58Prefixes[(int)Base58Type.ASSET_ID]);
            Assert.Equal(new byte[] { 0x13 }, network.Base58Prefixes[(int)Base58Type.COLORED_ADDRESS]);

            Assert.Equal(210000, network.Consensus.SubsidyHalvingInterval);
            Assert.Equal(750, network.Consensus.MajorityEnforceBlockUpgrade);
            Assert.Equal(950, network.Consensus.MajorityRejectBlockOutdated);
            Assert.Equal(1000, network.Consensus.MajorityWindow);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP34]);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP65]);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP66]);
            Assert.Equal(new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
                network.Consensus.BIP34Hash);
            Assert.Equal(new Target(new uint256("0000ffff00000000000000000000000000000000000000000000000000000000")),
                network.Consensus.PowLimit);
            Assert.Null(network.Consensus.MinimumChainWork);
            Assert.Equal(TimeSpan.FromSeconds(14 * 24 * 60 * 60), network.Consensus.PowTargetTimespan);
            Assert.Equal(TimeSpan.FromSeconds(10 * 60), network.Consensus.PowTargetSpacing);
            Assert.False(network.Consensus.PowAllowMinDifficultyBlocks);
            Assert.False(network.Consensus.PowNoRetargeting);
            Assert.Equal(1916, network.Consensus.RuleChangeActivationThreshold);
            Assert.Equal(2016, network.Consensus.MinerConfirmationWindow);
            Assert.Null(network.Consensus.BIP9Deployments[RedstoneBIP9Deployments.TestDummy]);
            Assert.Equal(1440, network.Consensus.LastPOWBlock);
            Assert.True(network.Consensus.IsProofOfStake);
            Assert.Equal(787264, network.Consensus.CoinType);
            Assert.Equal(
                new BigInteger(uint256.Parse("0000ffff00000000000000000000000000000000000000000000000000000000")
                    .ToBytes(false)), network.Consensus.ProofOfStakeLimit);
            Assert.Equal(
                new BigInteger(uint256.Parse("0000ffff00000000000000000000000000000000000000000000000000000000")
                    .ToBytes(false)), network.Consensus.ProofOfStakeLimitV2);
            Assert.Equal(new uint256("0x98fa6ef0bca5b431f15fd79dc6f879dc45b83ed4b1bbe933a383ef438321958e"),
                network.Consensus.DefaultAssumeValid);
            Assert.Equal(10, network.Consensus.CoinbaseMaturity);
            Assert.Equal(Money.Coins(30000), network.Consensus.PremineReward);
            Assert.Equal(2, network.Consensus.PremineHeight);
            Assert.Equal(Money.Coins(30), network.Consensus.ProofOfWorkReward);
            Assert.Equal(Money.Coins(15), network.Consensus.ProofOfStakeReward);
            Assert.True(network.Consensus.PosRewardReduction);
            Assert.Equal(2880, network.Consensus.PosRewardReductionBlockInterval);
            Assert.Equal(7.5m, network.Consensus.PosRewardReductionPercentage);
            Assert.Equal((uint)500, network.Consensus.MaxReorgLength);
            Assert.Equal(long.MaxValue, network.Consensus.MaxMoney);

            Block genesis = network.GetGenesis();
            Assert.Equal(uint256.Parse("0x5b3bce1db145b398f502782d4fbef62cbb46205a41bb4aa37cda3619729e3037"),
                genesis.GetHash());
            Assert.Equal(uint256.Parse("ad15198e3c12a1c342f346ca3a6e2faea6bfec7491e6143d636b8741a22ce2b9"),
                genesis.Header.HashMerkleRoot);
        }


        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void RedstoneRegTestIsInitializedCorrectly()
        {
            Network network = RedstoneNetworks.RegTest;

            Assert.Empty(network.Checkpoints);
            Assert.Empty(network.DNSSeeds);
            Assert.Empty(network.SeedNodes);

            Assert.Equal("RedstoneRegTest", network.Name);
            Assert.Equal(RedstoneMain.RedstoneRootFolderName, network.RootFolderName);
            Assert.Equal(RedstoneMain.RedstoneDefaultConfigFilename, network.DefaultConfigFilename);
            Assert.Equal(0xefc0f2cd, network.Magic);
            Assert.Equal(19256, network.DefaultPort);
            Assert.Equal(19257, network.RPCPort);
            Assert.Equal(RedstoneMain.RedstoneMaxTimeOffsetSeconds, network.MaxTimeOffsetSeconds);
            Assert.Equal(RedstoneMain.RedstoneDefaultMaxTipAgeInSeconds, network.MaxTipAge);
            Assert.Equal(0, network.MinTxFee);
            Assert.Equal(0, network.FallbackFee);
            Assert.Equal(0, network.MinRelayTxFee);
            Assert.Equal("TXRD", network.CoinTicker);

            Assert.Equal(2, network.Bech32Encoders.Length);
            Assert.Equal(new Bech32Encoder("bc").ToString(),
                network.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS].ToString());
            Assert.Equal(new Bech32Encoder("bc").ToString(),
                network.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS].ToString());

            Assert.Equal(12, network.Base58Prefixes.Length);
            Assert.Equal(new byte[] { (63) }, network.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS]);
            Assert.Equal(new byte[] { (196) }, network.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS]);
            Assert.Equal(new byte[] { (63 + 128) }, network.Base58Prefixes[(int)Base58Type.SECRET_KEY]);
            Assert.Equal(new byte[] { 0x01, 0x42 }, network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC]);
            Assert.Equal(new byte[] { 0x01, 0x43 }, network.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC]);
            Assert.Equal(new byte[] { (0x04), (0x88), (0xB2), (0x1E) },
                network.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY]);
            Assert.Equal(new byte[] { (0x04), (0x88), (0xAD), (0xE4) },
                network.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY]);
            Assert.Equal(new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 },
                network.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE]);
            Assert.Equal(new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A },
                network.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE]);
            Assert.Equal(new byte[] { 0x2a }, network.Base58Prefixes[(int)Base58Type.STEALTH_ADDRESS]);
            Assert.Equal(new byte[] { 23 }, network.Base58Prefixes[(int)Base58Type.ASSET_ID]);
            Assert.Equal(new byte[] { 0x13 }, network.Base58Prefixes[(int)Base58Type.COLORED_ADDRESS]);

            Assert.Equal(210000, network.Consensus.SubsidyHalvingInterval);
            Assert.Equal(750, network.Consensus.MajorityEnforceBlockUpgrade);
            Assert.Equal(950, network.Consensus.MajorityRejectBlockOutdated);
            Assert.Equal(1000, network.Consensus.MajorityWindow);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP34]);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP65]);
            Assert.Equal(0, network.Consensus.BuriedDeployments[BuriedDeployments.BIP66]);
            Assert.Equal(new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
                network.Consensus.BIP34Hash);
            Assert.Equal(new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                network.Consensus.PowLimit);
            Assert.Null(network.Consensus.MinimumChainWork);
            Assert.Equal(TimeSpan.FromSeconds(14 * 24 * 60 * 60), network.Consensus.PowTargetTimespan);
            Assert.Equal(TimeSpan.FromSeconds(10 * 60), network.Consensus.PowTargetSpacing);
            Assert.True(network.Consensus.PowAllowMinDifficultyBlocks);
            Assert.True(network.Consensus.PowNoRetargeting);
            Assert.Equal(1916, network.Consensus.RuleChangeActivationThreshold);
            Assert.Equal(2016, network.Consensus.MinerConfirmationWindow);
            Assert.Null(network.Consensus.BIP9Deployments[RedstoneBIP9Deployments.TestDummy]);
            Assert.Equal(1440, network.Consensus.LastPOWBlock);
            Assert.True(network.Consensus.IsProofOfStake);
            Assert.Equal(787264, network.Consensus.CoinType);
            Assert.Equal(
                new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")
                    .ToBytes(false)), network.Consensus.ProofOfStakeLimit);
            Assert.Equal(
                new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff")
                    .ToBytes(false)), network.Consensus.ProofOfStakeLimitV2);
            Assert.Null(network.Consensus.DefaultAssumeValid);
            Assert.Equal(10, network.Consensus.CoinbaseMaturity);
            Assert.Equal(Money.Coins(30000), network.Consensus.PremineReward);
            Assert.Equal(2, network.Consensus.PremineHeight);
            Assert.Equal(Money.Coins(30), network.Consensus.ProofOfWorkReward);
            Assert.Equal(Money.Coins(15), network.Consensus.ProofOfStakeReward);
            Assert.True(network.Consensus.PosRewardReduction);
            Assert.Equal(2880, network.Consensus.PosRewardReductionBlockInterval);
            Assert.Equal(7.5m, network.Consensus.PosRewardReductionPercentage);
            Assert.Equal((uint)500, network.Consensus.MaxReorgLength);
            Assert.Equal(long.MaxValue, network.Consensus.MaxMoney);

            Block genesis = network.GetGenesis();
            Assert.Equal(uint256.Parse("0x4442244290302b76a11951ba648e40bcdc6cc3965a99c30018ade95a4bc6e6bd"),
                genesis.GetHash());
            Assert.Equal(uint256.Parse("0x0ad80b454d4060b0f9bc821d94ec14da59ea7194a9fd3875b31c14873d202b7d"),
                genesis.Header.HashMerkleRoot);
        }
    }
}