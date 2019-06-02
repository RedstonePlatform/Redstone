using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.ServiceNode.Events;
using Redstone.ServiceNode.Models;
using Stratis.Bitcoin.EventBus;
using Stratis.Bitcoin.Features.BlockStore.AddressIndexing;
using Stratis.Bitcoin.Signals;

namespace Redstone.Features.ServiceNode
{
    /// <summary>Class that checks if service nodes fulfill the collateral requirement.</summary>
    public class ServiceNodeCollateralChecker : IServiceNodeCollateralChecker, IDisposable
    {
        private readonly IAddressIndexer addressIndexer;
        private readonly Network network;
        private readonly IServiceNodeManager serviceNodeManager;
        private readonly ISignals signals;

        private readonly ILogger logger;

        /// <summary>Protects access to <see cref="depositsByAddress"/>.</summary>
        private readonly object locker = new object();

        private readonly CancellationTokenSource cancellationSource = new CancellationTokenSource();

        private SubscriptionToken serviceNodeAddedToken, serviceNodeKickedToken;

        /// <summary>Amount of confirmations required for collateral.</summary>
        private const int RequiredConfirmations = 1;

        private const int CollateralInitializationUpdateIntervalSeconds = 3;

        private const int CollateralUpdateIntervalSeconds = 20;

        /// <summary>Deposits mapped by service node.</summary>
        /// <remarks>
        /// Deposits are not updated if service node doesn't have collateral requirement enabled.
        /// All access should be protected by <see cref="locker"/>.
        /// </remarks>
        private readonly Dictionary<string, Money> depositsByAddress;

        private Task updateCollateralContinuouslyTask;

        public ServiceNodeCollateralChecker(
            Network network,
            ILoggerFactory loggerFactory,
            IServiceNodeManager serviceNodeManager,
            ISignals signals,
            IAddressIndexer addressIndexer)
        {
            this.network = network;
            this.serviceNodeManager = serviceNodeManager;
            this.signals = signals;
            this.addressIndexer = addressIndexer;
            this.depositsByAddress = new Dictionary<string, Money>();
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        public async Task InitializeAsync()
        {
            this.serviceNodeAddedToken = this.signals.Subscribe<ServiceNodeAdded>(this.OnServiceNodeAdded);
            this.serviceNodeKickedToken = this.signals.Subscribe<ServiceNodeRemoved>(this.OnServiceNodeKicked);

            IEnumerable<IServiceNode> serviceNodes = this.serviceNodeManager.GetServiceNodes();

            foreach (IServiceNode serviceNode in serviceNodes)
            {
                lock (this.locker)
                {
                    this.depositsByAddress.Add(serviceNode.CollateralAddress, null);
                }
            }

            while (true)
            {
                bool success = this.UpdateCollateralInfo(this.cancellationSource.Token);

                if (!success)
                {
                    // TODO :AC
                    this.logger.LogWarning("Failed to update collateral.");

                    try
                    {
                        await Task.Delay(CollateralInitializationUpdateIntervalSeconds * 1000, this.cancellationSource.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        this.logger.LogTrace("(-)[CANCELLED]");
                        return;
                    }
                }
                else
                {
                    break;
                }
            }

            this.updateCollateralContinuouslyTask = this.UpdateCollateralInfoContinuouslyAsync();
        }

        private async Task UpdateCollateralInfoContinuouslyAsync()
        {
            while (!this.cancellationSource.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(CollateralUpdateIntervalSeconds * 1000, this.cancellationSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    this.logger.LogTrace("(-)[CANCELLED]");
                    return;
                }

                bool success = this.UpdateCollateralInfo(this.cancellationSource.Token);

                if (!success)
                    this.logger.LogWarning("Failed to update collateral.");
            }
        }

        private bool UpdateCollateralInfo(CancellationToken cancellation = default)
        {
            List<string> addressesToCheck;

            lock (this.locker)
            {
                addressesToCheck = this.depositsByAddress.Keys.ToList();
            }

            if (addressesToCheck.Count == 0)
            {
                this.logger.LogTrace("(-)[NOTHING_TO_CHECK]:true");
                return true;
            }

            var collateral = new Dictionary<string, Money>();

            foreach (string addressToCheck in addressesToCheck)
            {
                if (cancellation.IsCancellationRequested)
                {
                    this.logger.LogTrace("(-)[CANCELLED]:false");
                    return false;
                }

                Money balance = this.addressIndexer.GetAddressBalance(addressToCheck, RequiredConfirmations);
                collateral.AddOrReplace(addressToCheck, balance);
            }

            if (collateral.Count != addressesToCheck.Count)
            {
                this.logger.LogTrace("(-)[INCONSISTENT_DATA]:false");
                return false;
            }

            lock (this.locker)
            {
                foreach ((string key, Money value) in collateral)
                {
                    this.depositsByAddress[key] = value;
                }
            }

            return true;
        }

        //private async Task CheckCollateralAsync(int height)
        //{
        //    foreach (IServiceNode serviceNode in this.serviceNodeManager.GetServiceNodes())
        //    {
        //        try
        //        {
        //            Script scriptToCheck = BitcoinAddress.Create(serviceNode.RegistrationRecord.Token.ServerId, this.network).ScriptPubKey;


        //            Money serverCollateralBalance =
        //                //await this.blockStoreClient.GetAddressBalanceAsync(registractionRecord.Token.ServerId, 1);
        //                this.addressIndexer.GetAddressBalance(serviceNode.RegistrationRecord.Token.ServerId, 1);

        //            this.logger.LogDebug("Collateral balance for server " + serviceNode.RegistrationRecord.Token.ServerId + " is " +
        //                                 serverCollateralBalance.ToString() + ", original registration height " +
        //                                 serviceNode.RegistrationRecord.BlockReceived + ", current height " + height);

        //            if ((serverCollateralBalance.ToUnit(MoneyUnit.BTC) < this.network.Consensus.ServiceNodeCollateralThreshold) &&
        //                ((height - serviceNode.RegistrationRecord.BlockReceived) > this.network.Consensus.ServiceNodeCollateralBlockPeriod))
        //            {
        //                // Remove server registrations as funding has not been performed within block count,
        //                // or funds have been removed from the collateral address subsequent to the
        //                // registration being performed
        //                this.logger.LogDebug("Insufficient collateral within window period for server: " + serviceNode.RegistrationRecord.Token.ServerId);
        //                this.logger.LogDebug("Deleting registration records for server: " + serviceNode.RegistrationRecord.Token.ServerId);

        //                this.serviceNodeManager.RemoveServiceNode(serviceNode);

        //                // TODO: Need to make the TumbleBitFeature change its server address if this is the address it was using
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            this.logger.LogError("Error calculating server collateral balance: " + e);
        //        }
        //    }
        //}

        public bool CheckCollateral(IServiceNode serviceNode)
        {
            lock (this.locker)
            {
                return this.depositsByAddress[serviceNode.CollateralAddress] >= this.network.Consensus.ServiceNodeCollateralThreshold;
            }
        }

        private void OnServiceNodeKicked(ServiceNodeRemoved serviceNodeRemoved)
        {
            lock (this.locker)
            {
                this.depositsByAddress.Remove(serviceNodeRemoved.RemovedNode.CollateralAddress);
            }
        }

        private void OnServiceNodeAdded(ServiceNodeAdded serviceNodeAdded)
        {
            lock (this.locker)
            {
                this.depositsByAddress.Add(serviceNodeAdded.AddedNode.CollateralAddress, null);
            }
        }

        public void Dispose()
        {
            this.signals.Unsubscribe(this.serviceNodeAddedToken);
            this.signals.Unsubscribe(this.serviceNodeKickedToken);

            this.cancellationSource.Cancel();

            this.updateCollateralContinuouslyTask?.GetAwaiter().GetResult();
        }
    }
}