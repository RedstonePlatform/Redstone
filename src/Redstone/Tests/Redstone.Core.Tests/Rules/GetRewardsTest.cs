using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;

namespace Redstone.Core.Tests.Rules
{
    using Stratis.Bitcoin.Features.Consensus.Rules;
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



            //15.00
            //13.88
            //12.83
            //11.87
            //10.98
            //10.16
            //9.40
            //8.69
            //8.04
            //7.44
            //6.88
            //6.36
            //5.89
            //5.44
            //5.04
            //4.66
            //4.31
            //3.99
            //3.69
            //3.41
            //3.15
            //2.92
            //2.70
            //2.50
        }
    }
}
