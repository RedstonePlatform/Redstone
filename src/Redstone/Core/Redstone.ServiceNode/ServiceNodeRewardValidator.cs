//using System.Linq;
//using Microsoft.Extensions.Logging;
//using NBitcoin;
//using Redstone.ServiceNode;
//using Stratis.Bitcoin.Consensus;

//namespace Stratis.Bitcoin.Features.Consensus.Interfaces
//{
//    public class ServiceNodeRewardValidator : IRewardValidator
//    {
//        private readonly Network network;
//        private readonly IConsensus consensus;
//        private readonly ILogger logger;

//        public ServiceNodeRewardValidator(Network network, IServiceNodeManager serviceNodeManager, ILoggerFactory loggerFactory)
//        {
//            this.network = network;
//            this.consensus = network.Consensus;
//            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
//        }

//        public void CheckReward(Transaction transaction, Money expectedStakeReward)
//        {
//            if (this.consensus.PosRewardMinterPercentage == 1)
//                return;

//            var groupedOutputs = transaction.Outputs.Where(o => !o.IsEmpty).GroupBy(o => o.ScriptPubKey);

//            if (groupedOutputs.Count() < 2 || groupedOutputs.Count() > 3)
//            {
//                this.logger.LogTrace("(-)[BAD_COINSTAKE_SPLIT_TOOMANY]");
//                ConsensusErrors.BadCoinbaseAmount.Throw();
//            }

//            var foundationOutput = groupedOutputs.Single(o => IsScriptPayToFoundation(o.Key));
//            long expectedFoundationReward = (long)(expectedStakeReward.Satoshi * this.consensus.PosRewardFoundationPercentage);
//            var foundationTotalOut = foundationOutput.Sum(txOut => txOut.Value.Satoshi);

//            if (foundationTotalOut < expectedFoundationReward)
//            {
//                this.logger.LogTrace("(-)[BAD_COINSTAKE_FOUNDATION_AMOUNT]");
//                ConsensusErrors.BadCoinstakeAmount.Throw();
//            }

//            var otherOuts = groupedOutputs.Where(o => !IsScriptPayToFoundation(o.Key)).ToList();

//            // TODO: check one of them is in the the top 10 service nodes
//            // TODO: need to prevent servicenode from minting in here and minting code
//            if (otherOuts.Count > 1) // Normal reward
//            {
//                var otherOut1Total = otherOuts[0].Sum(txOut => txOut.Value.Satoshi);
//                var otherOut2Total = otherOuts[1].Sum(txOut => txOut.Value.Satoshi);

//                long expectedServiceNodeReward = (long)(expectedStakeReward.Satoshi * this.consensus.PosRewardServiceNodePercentage);
//                long expectedMinterReward = (long)(expectedStakeReward.Satoshi * this.consensus.PosRewardMinterPercentage);

//                if ((otherOut1Total < expectedServiceNodeReward && otherOut2Total < expectedServiceNodeReward)
//                    && (otherOut1Total < expectedMinterReward && otherOut2Total < expectedMinterReward))
//                {
//                    this.logger.LogTrace("(-)[BAD_COINSTAKE_OTHER_AMOUNT]");
//                    ConsensusErrors.BadCoinstakeAmount.Throw();
//                }
//            }
//            else // No Services Nodes (Foundation receives reward)
//            {
//                // Already validated
//            }
//        }

//        private bool IsScriptPayToFoundation(Script script)
//        {
//            var dest = script.GetDestinationAddress(this.network);
//            return dest == new KeyId(this.consensus.PosRewardFoundationPubKeyHash).GetAddress(this.network);
//        }
//    }
//}