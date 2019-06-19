using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Consensus.Rules;
using Stratis.Bitcoin.Features.Consensus.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus.Rules.CommonRules
{
    /// <summary>
    /// Proof of stake override for the coinview rules - BIP68, MaxSigOps and BlockReward checks.
    /// </summary>
    public sealed class ServiceNodePosCoinviewRule : CoinViewRule
    {
        /// <summary>Provides functionality for checking validity of PoS blocks.</summary>
        private IStakeValidator stakeValidator;

        /// <summary>Database of stake related data for the current blockchain.</summary>
        private IStakeChain stakeChain;

        /// <summary>The consensus of the parent Network.</summary>
        private IConsensus consensus;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            this.consensus = this.Parent.Network.Consensus;
            var consensusRules = (PosConsensusRuleEngine)this.Parent;

            this.stakeValidator = consensusRules.StakeValidator;
            this.stakeChain = consensusRules.StakeChain;
        }

        /// <inheritdoc />
        /// <summary>Compute and store the stake proofs.</summary>
        public override async Task RunAsync(RuleContext context)
        {
            this.CheckAndComputeStake(context);

            await base.RunAsync(context).ConfigureAwait(false);
            var posRuleContext = context as PosRuleContext;
            this.stakeChain.Set(context.ValidationContext.ChainedHeaderToValidate, posRuleContext.BlockStake);
        }

        /// <inheritdoc />
        public override void CheckBlockReward(RuleContext context, Money fees, int height, Block block)
        {
            if (BlockStake.IsProofOfStake(block))
            {
                var posRuleContext = context as PosRuleContext;
                Money stakeReward = block.Transactions[1].TotalOut - posRuleContext.TotalCoinStakeValueIn;
                Money calcStakeReward = fees + this.GetProofOfStakeReward(height);

                this.Logger.LogTrace("Block stake reward is {0}, calculated reward is {1}.", stakeReward, calcStakeReward);
                if (stakeReward > calcStakeReward)
                {
                    this.Logger.LogTrace("(-)[BAD_COINSTAKE_AMOUNT]");
                    ConsensusErrors.BadCoinstakeAmount.Throw();
                }

                try
                {
                    CheckRewardSplit(block.Transactions[1], stakeReward);
                }
                catch
                {
                    this.Logger.LogTrace("(-)[BAD_COINSTAKE_SPLIT]");
                    ConsensusErrors.BadCoinstakeAmount.Throw();
                }
            }
            else
            {
                Money blockReward = fees + this.GetProofOfWorkReward(height);
                this.Logger.LogTrace("Block reward is {0}, calculated reward is {1}.", block.Transactions[0].TotalOut, blockReward);
                if (block.Transactions[0].TotalOut > blockReward)
                {
                    this.Logger.LogTrace("(-)[BAD_COINBASE_AMOUNT]");
                    ConsensusErrors.BadCoinbaseAmount.Throw();
                }
            }
        }

        private void CheckRewardSplit(Transaction transaction, Money expectedStakeReward)
        {
            if (this.consensus.PosRewardMinterPercentage == 1)
                return;

            var groupedOutputs = transaction.Outputs.Where(o => !o.IsEmpty).GroupBy(o => o.ScriptPubKey);

            if (groupedOutputs.Count() < 2 || groupedOutputs.Count() > 3)
            {
                this.Logger.LogTrace("(-)[BAD_COINSTAKE_SPLIT_TOOMANY]");
                ConsensusErrors.BadCoinbaseAmount.Throw();
            }

            var foundationOutput = groupedOutputs.Single(o => IsScriptPayToFoundation(o.Key));
            long expectedFoundationReward = (long)(expectedStakeReward.Satoshi * this.consensus.PosRewardFoundationPercentage);
            var foundationTotalOut = foundationOutput.Sum(txOut => txOut.Value.Satoshi);

            if (foundationTotalOut < expectedFoundationReward)
            {
                this.Logger.LogTrace("(-)[BAD_COINSTAKE_FOUNDATION_AMOUNT]");
                ConsensusErrors.BadCoinstakeAmount.Throw();
            }

            var otherOuts = groupedOutputs.Where(o => !IsScriptPayToFoundation(o.Key)).ToList();

            // TODO: check one of them is in the the top 10 service nodes
            // TODO: need to prevent servicenode from minting in here and minting code
            if (otherOuts.Count > 1) // Normal reward
            {
                var otherOut1Total = otherOuts[0].Sum(txOut => txOut.Value.Satoshi);
                var otherOut2Total = otherOuts[1].Sum(txOut => txOut.Value.Satoshi);

                long expectedServiceNodeReward = (long)(expectedStakeReward.Satoshi * this.consensus.PosRewardServiceNodePercentage);
                long expectedMinterReward = (long)(expectedStakeReward.Satoshi * this.consensus.PosRewardMinterPercentage);

                if ((otherOut1Total < expectedServiceNodeReward && otherOut2Total < expectedServiceNodeReward)
                    && (otherOut1Total < expectedMinterReward && otherOut2Total < expectedMinterReward))
                {
                    this.Logger.LogTrace("(-)[BAD_COINSTAKE_OTHER_AMOUNT]");
                    ConsensusErrors.BadCoinstakeAmount.Throw();
                }
            }
            else // No Services Nodes (Foundation receives reward)
            {
                // Already validated
            }
        }

        private bool IsScriptPayToFoundation(Script script)
        {
            var dest = script.GetDestinationAddress(this.Parent.Network);
            return dest == new KeyId(this.consensus.PosRewardFoundationPubKeyHash).GetAddress(this.Parent.Network);
        }

        protected override Money GetTransactionFee(UnspentOutputSet view, Transaction tx)
        {
            return tx.IsCoinStake ? Money.Zero : view.GetValueIn(tx) - tx.TotalOut;
        }

        /// <inheritdoc />
        public override void UpdateCoinView(RuleContext context, Transaction transaction)
        {
            var posRuleContext = context as PosRuleContext;

            UnspentOutputSet view = posRuleContext.UnspentOutputSet;

            if (transaction.IsCoinStake)
            {
                posRuleContext.TotalCoinStakeValueIn = view.GetValueIn(transaction);
                posRuleContext.CoinStakePrevOutputs = transaction.Inputs.ToDictionary(txin => txin, txin => view.GetOutputFor(txin));
            }

            base.UpdateUTXOSet(context, transaction);
        }

        /// <inheritdoc />
        public override void CheckMaturity(UnspentOutputs coins, int spendHeight)
        {
            base.CheckCoinbaseMaturity(coins, spendHeight);

            if (coins.IsCoinstake)
            {
                if ((spendHeight - coins.Height) < this.consensus.CoinbaseMaturity)
                {
                    this.Logger.LogTrace("Coinstake transaction height {0} spent at height {1}, but maturity is set to {2}.", coins.Height, spendHeight, this.consensus.CoinbaseMaturity);
                    this.Logger.LogTrace("(-)[COINSTAKE_PREMATURE_SPENDING]");
                    ConsensusErrors.BadTransactionPrematureCoinstakeSpending.Throw();
                }
            }
        }

        /// <inheritdoc />
        protected override void CheckInputValidity(Transaction transaction, UnspentOutputs coins)
        {
            // Transaction timestamp earlier than input transaction - main.cpp, CTransaction::ConnectInputs
            if (coins.Time > transaction.Time)
                ConsensusErrors.BadTransactionEarlyTimestamp.Throw();
        }

        /// <summary>
        /// Checks and computes stake.
        /// </summary>
        /// <param name="context">Context that contains variety of information regarding blocks validation and execution.</param>
        /// <exception cref="ConsensusErrors.PrevStakeNull">Thrown if previous stake is not found.</exception>
        /// <exception cref="ConsensusErrors.SetStakeEntropyBitFailed">Thrown if failed to set stake entropy bit.</exception>
        private void CheckAndComputeStake(RuleContext context)
        {
            ChainedHeader chainedHeader = context.ValidationContext.ChainedHeaderToValidate;
            Block block = context.ValidationContext.BlockToValidate;

            var posRuleContext = context as PosRuleContext;
            if (posRuleContext.BlockStake == null)
                posRuleContext.BlockStake = BlockStake.Load(context.ValidationContext.BlockToValidate);

            BlockStake blockStake = posRuleContext.BlockStake;

            // Verify hash target and signature of coinstake tx.
            if (BlockStake.IsProofOfStake(block))
            {
                ChainedHeader prevChainedHeader = chainedHeader.Previous;

                BlockStake prevBlockStake = this.stakeChain.Get(prevChainedHeader.HashBlock);
                if (prevBlockStake == null)
                    ConsensusErrors.PrevStakeNull.Throw();

                // Only do proof of stake validation for blocks that are after the assumevalid block or after the last checkpoint.
                if (!context.SkipValidation)
                {
                    this.stakeValidator.CheckProofOfStake(posRuleContext, prevChainedHeader, prevBlockStake, block.Transactions[1], chainedHeader.Header.Bits.ToCompact());
                }
                else this.Logger.LogTrace("POS validation skipped for block at height {0}.", chainedHeader.Height);
            }

            // PoW is checked in CheckBlock().
            if (BlockStake.IsProofOfWork(block))
                posRuleContext.HashProofOfStake = chainedHeader.Header.GetPoWHash();

            // Compute stake entropy bit for stake modifier.
            if (!blockStake.SetStakeEntropyBit(blockStake.GetStakeEntropyBit()))
            {
                this.Logger.LogTrace("(-)[STAKE_ENTROPY_BIT_FAIL]");
                ConsensusErrors.SetStakeEntropyBitFailed.Throw();
            }

            // Record proof hash value.
            blockStake.HashProof = posRuleContext.HashProofOfStake;

            int lastCheckpointHeight = this.Parent.Checkpoints.GetLastCheckpointHeight();
            if (chainedHeader.Height > lastCheckpointHeight)
            {
                // Compute stake modifier.
                ChainedHeader prevChainedHeader = chainedHeader.Previous;
                BlockStake blockStakePrev = prevChainedHeader == null ? null : this.stakeChain.Get(prevChainedHeader.HashBlock);
                blockStake.StakeModifierV2 = this.stakeValidator.ComputeStakeModifierV2(prevChainedHeader, blockStakePrev?.StakeModifierV2, blockStake.IsProofOfWork() ? chainedHeader.HashBlock : blockStake.PrevoutStake.Hash);
            }
            else if (chainedHeader.Height == lastCheckpointHeight)
            {
                // Copy checkpointed stake modifier.
                CheckpointInfo checkpoint = this.Parent.Checkpoints.GetCheckpoint(lastCheckpointHeight);
                blockStake.StakeModifierV2 = checkpoint.StakeModifierV2;
                this.Logger.LogTrace("Last checkpoint stake modifier V2 loaded: '{0}'.", blockStake.StakeModifierV2);
            }
            else this.Logger.LogTrace("POS stake modifier computation skipped for block at height {0} because it is not above last checkpoint block height {1}.", chainedHeader.Height, lastCheckpointHeight);
        }

        /// <inheritdoc />
        public override Money GetProofOfWorkReward(int height)
        {
            if (this.IsPremine(height))
                return this.consensus.PremineReward;

            return this.consensus.ProofOfWorkReward;
        }

        /// <summary>
        /// Gets miner's coin stake reward.
        /// </summary>
        /// <param name="height">Target block height.</param>
        /// <returns>Miner's coin stake reward.</returns>
        public Money GetProofOfStakeReward(int height)
        {
            if (this.IsPremine(height))
                return this.consensus.PremineReward;

            if (this.consensus.PosRewardReduction)
            {
                int blockIntervals = height / this.consensus.PosRewardReductionBlockInterval;
                double reductionRate = (double)((100 - this.consensus.PosRewardReductionPercentage) / 100);
                double reward = this.consensus.ProofOfStakeReward.Satoshi * Math.Pow(reductionRate, blockIntervals);
                return new Money((long)reward);

            }
            else
                return this.consensus.ProofOfStakeReward;
        }
    }
}
