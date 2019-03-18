using System;
using NBitcoin;

namespace Redstone.Features.ServiceNode
{
    public interface IRegistrationManager : IDisposable
    {
        /// <summary>
        /// Processes a block received from the network.
        /// </summary>
        /// <param name="height">The height of the block in the blockchain.</param>
        /// <param name="block">The block.</param>
        void ProcessBlock(int height, Block block);
    }
}
