using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.ServiceNode;
using Stratis.Bitcoin.Consensus;

namespace Stratis.Bitcoin.Features.Consensus.Interfaces
{
    public class FoundationRewardValidator : IRewardValidator
    {
        private readonly Network network;
        private readonly IConsensus consensus;
        private readonly ILogger logger;

        public FoundationRewardValidator(Network network, IServiceNodeManager serviceNodeManager, ILoggerFactory loggerFactory)
        {
            this.network = network;
            this.consensus = network.Consensus;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        public void CheckReward(Transaction transaction, Money expectedStakeReward)
        {
            if (this.consensus.PosRewardMinterPercentage == 1)
                return;

            var groupedOutputs = transaction.Outputs.Where(o => !o.IsEmpty).GroupBy(o => o.ScriptPubKey);

            if (groupedOutputs.Count() < 2)
            {
                this.logger.LogTrace("(-)[BAD_COINSTAKE_SPLIT]");
                ConsensusErrors.BadCoinbaseAmount.Throw();
            }

            var foundationOutput = groupedOutputs.Single(o => IsScriptPayToFoundation(o.Key));
            long expectedFoundationReward = (long)(expectedStakeReward.Satoshi * this.consensus.PosRewardFoundationPercentage);
            var foundationTotalOut = foundationOutput.Sum(txOut => txOut.Value.Satoshi);

            if (foundationTotalOut < expectedFoundationReward)
            {
                this.logger.LogTrace("(-)[BAD_COINSTAKE_FOUNDATION_AMOUNT]");
                ConsensusErrors.BadCoinstakeAmount.Throw();
            }
        }

        private bool IsScriptPayToFoundation(Script script)
        {
            var dest = script.GetDestinationAddress(this.network);
            return dest == new KeyId(this.consensus.PosRewardFoundationPubKeyHash).GetAddress(this.network);
        }
    }
}
