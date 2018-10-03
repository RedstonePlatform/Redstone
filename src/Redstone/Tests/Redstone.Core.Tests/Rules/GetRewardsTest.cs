namespace Redstone.Core.Tests.Rules
{
    using Microsoft.Extensions.Logging;
    using Moq;
    using NBitcoin;
    using Stratis.Bitcoin.Features.Consensus.Rules.CommonRules;
    using Xunit;

    public class GetRewardsTests : TestPosConsensusRulesUnitTestBase
    {
        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void Test()
        {
            var posCoinviewRule = new PosCoinviewRule { Parent = this.InitializeConsensusRules() };
            posCoinviewRule.Logger = new Mock<ILogger>().Object;
            posCoinviewRule.Initialize();

            var network = posCoinviewRule.Parent.Network;
            var blockInterval = network.Consensus.PosRewardReductionBlockInterval;

            Assert.Equal(new Money(15, MoneyUnit.BTC), posCoinviewRule.GetProofOfStakeReward(500));
            Assert.Equal(new Money(13.875m, MoneyUnit.BTC), posCoinviewRule.GetProofOfStakeReward(500 + blockInterval));
            Assert.Equal(new Money(12.83437500m, MoneyUnit.BTC), posCoinviewRule.GetProofOfStakeReward(500 + (blockInterval * 2)));
            Assert.Equal(new Money(11.87179687m, MoneyUnit.BTC), posCoinviewRule.GetProofOfStakeReward(500 + (blockInterval * 3)));
            Assert.Equal(new Money(10.98141210m, MoneyUnit.BTC), posCoinviewRule.GetProofOfStakeReward(500 + (blockInterval * 4)));
            Assert.Equal(new Money(10.15780620m, MoneyUnit.BTC), posCoinviewRule.GetProofOfStakeReward(500 + (blockInterval * 5)));
        }
    }
}
