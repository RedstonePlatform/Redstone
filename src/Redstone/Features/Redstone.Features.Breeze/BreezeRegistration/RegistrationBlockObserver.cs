using System;
using NBitcoin;
using Stratis.Bitcoin.Primitives;
using Stratis.Bitcoin.Signals;
using Stratis.Bitcoin.Utilities;

namespace Redstone.Features.Breeze.BreezeRegistration
{
    /// <summary>
    /// Observer that receives notifications about the arrival of new <see cref="ChainedHeaderBlock"/>s.
    /// </summary>
    public class RegistrationBlockObserver : SignalObserver<ChainedHeaderBlock>
    {
        private readonly ConcurrentChain chain;
        private readonly IRegistrationManager registrationManager;

        public RegistrationBlockObserver(ConcurrentChain chain, IRegistrationManager registrationManager)
        {
            this.chain = chain;
            this.registrationManager = registrationManager;
        }

        protected override void OnErrorCore(Exception error)
        {
            Guard.NotNull(error, nameof(error));
            // Nothing to do.
        }

        /// <summary>
        /// Manages what happens when a new block is received.
        /// </summary>
        /// <param name="block">The new block</param>
        protected override void OnNextCore(ChainedHeaderBlock block)
        {
            var hash = block.Block.Header.GetHash();
            var height = this.chain.GetBlock(hash).Height;

            this.registrationManager.ProcessBlock(height, block.Block);
        }
    }
}
