using System;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.Features.ServiceNode.Common;
using Stratis.Bitcoin.Features.WatchOnlyWallet;

namespace Redstone.Features.ServiceNode
{
    public interface IRegistrationManager : IDisposable
    {
        void Initialize(ILoggerFactory loggerFactory, RegistrationStore registrationStore, bool isBitcoin, Network network, IWatchOnlyWalletManager watchOnlyWalletManager);

        /// <summary>
        /// Processes a block received from the network.
        /// </summary>
        /// <param name="height">The height of the block in the blockchain.</param>
        /// <param name="block">The block.</param>
        void ProcessBlock(int height, Block block);
    }
}
