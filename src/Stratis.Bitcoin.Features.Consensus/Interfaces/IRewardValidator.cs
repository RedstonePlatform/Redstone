using NBitcoin;

namespace Stratis.Bitcoin.Features.Consensus.Interfaces
{
    public interface IRewardValidator
    {
        void CheckReward(Transaction transaction, Money expectedStakeReward);
    }

    public class PosRewardValidator : IRewardValidator
    {
        public void CheckReward(Transaction transaction, Money expectedStakeReward)
        {
            // noop
            // TODO: move coinviewrule reward checks into here
        }
    }
}
