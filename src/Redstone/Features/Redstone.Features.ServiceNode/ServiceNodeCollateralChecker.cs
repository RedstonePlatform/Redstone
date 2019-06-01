//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using NBitcoin;
//using Redstone.ServiceNode.Events;
//using Redstone.ServiceNode.Models;
//using Stratis.Bitcoin.EventBus;
//using Stratis.Bitcoin.Features.Api;
//using Stratis.Bitcoin.Features.BlockStore.Controllers;
//using Stratis.Bitcoin.Signals;

//namespace Redstone.Features.ServiceNode
//{
//    /// <summary>Class that checks if service nodes fulfill the collateral requirement.</summary>
//    public class ServiceNodeCollateralChecker : IDisposable
//    {
//        private readonly IBlockStoreClient blockStoreClient;
//        private readonly IServiceNodeManager serviceNodeManager;
//        private readonly ISignals signals;

//        private readonly ILogger logger;

//        /// <summary>Protects access to <see cref="depositsByAddress"/>.</summary>
//        private readonly object locker = new object();

//        private readonly CancellationTokenSource cancellationSource = new CancellationTokenSource();

//        private SubscriptionToken serviceNodeAddedToken, serviceNodeKickedToken;

//        /// <summary>Amount of confirmations required for collateral.</summary>
//        private const int RequiredConfirmations = 1;

//        private const int CollateralInitializationUpdateIntervalSeconds = 3;

//        private const int CollateralUpdateIntervalSeconds = 20;

//        /// <summary>Deposits mapped by service node.</summary>
//        /// <remarks>
//        /// Deposits are not updated if service node doesn't have collateral requirement enabled.
//        /// All access should be protected by <see cref="locker"/>.
//        /// </remarks>
//        private Dictionary<string, Money> depositsByAddress;

//        private Task updateCollateralContinuouslyTask;

//        public ServiceNodeCollateralChecker(
//            ILoggerFactory loggerFactory, 
//            IHttpClientFactory httpClientFactory, 
//            IServiceNodeManager serviceNodeManager, 
//            ISignals signals, 
//            ApiSettings settings)
//        {
//            this.serviceNodeManager = serviceNodeManager;
//            this.signals = signals;
//            this.depositsByAddress = new Dictionary<string, Money>();
//            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
//            //this.blockStoreClient = new BlockStoreClient(loggerFactory, httpClientFactory, settings.ApiPort);
//        }

//        public async Task InitializeAsync()
//        {
//            this.serviceNodeAddedToken = this.signals.Subscribe<ServiceNodeAdded>(this.OnServiceNodeAdded);
//            this.serviceNodeKickedToken = this.signals.Subscribe<ServiceNodeRemoved>(this.OnServiceNodeKicked);

//            IEnumerable<IServiceNode> serviceNodes = this.serviceNodeManager.GetServiceNodes().Cast<IServiceNode>()
//                .Where(x => x.CollateralAmount != null && x.CollateralAmount > 0);

//            foreach (IServiceNode serviceNode in serviceNodes)
//            {
//                this.depositsByAddress.Add(serviceNode.CollateralMainchainAddress, null);
//            }

//            while (true)
//            {
//                bool success = await this.UpdateCollateralInfoAsync(this.cancellationSource.Token).ConfigureAwait(false);

//                this.logger.LogWarning("Failed to update collateral. Ensure that mainnet gateway node is running and API is enabled. " +
//                                       "Node will not continue initialization before another gateway node responds.");

//                if (!success)
//                {
//                    try
//                    {
//                        await Task.Delay(CollateralInitializationUpdateIntervalSeconds * 1000, this.cancellationSource.Token).ConfigureAwait(false);
//                    }
//                    catch (OperationCanceledException)
//                    {
//                        this.logger.LogTrace("(-)[CANCELLED]");
//                        return;
//                    }
//                }
//                else
//                {
//                    break;
//                }
//            }

//            this.updateCollateralContinuouslyTask = this.UpdateCollateralInfoContinuouslyAsync();
//        }

//        private async Task UpdateCollateralInfoContinuouslyAsync()
//        {
//            while (!this.cancellationSource.Token.IsCancellationRequested)
//            {
//                try
//                {
//                    await Task.Delay(CollateralUpdateIntervalSeconds * 1000, this.cancellationSource.Token).ConfigureAwait(false);
//                }
//                catch (OperationCanceledException)
//                {
//                    this.logger.LogTrace("(-)[CANCELLED]");
//                    return;
//                }

//                bool success = await this.UpdateCollateralInfoAsync(this.cancellationSource.Token).ConfigureAwait(false);

//                if (!success)
//                    this.logger.LogWarning("Failed to update collateral. Ensure that servicenode is running and API is enabled.");
//            }
//        }

//        private async Task<bool> UpdateCollateralInfoAsync(CancellationToken cancellation = default(CancellationToken))
//        {
//            List<string> addressesToCheck;

//            lock (this.locker)
//            {
//                addressesToCheck = this.depositsByAddress.Keys.ToList();
//            }

//            if (addressesToCheck.Count == 0)
//            {
//                this.logger.LogTrace("(-)[NOTHING_TO_CHECK]:true");
//                return true;
//            }

//            Dictionary<string, Money> collateral = await this.blockStoreClient.GetAddressBalancesAsync(addressesToCheck, RequiredConfirmations, cancellation).ConfigureAwait(false);

//            if (collateral == null)
//            {
//                this.logger.LogTrace("(-)[FAILED]:false");
//                return false;
//            }

//            if (collateral.Count != addressesToCheck.Count)
//            {
//                this.logger.LogTrace("(-)[INCONSISTENT_DATA]:false");
//                return false;
//            }

//            lock (this.locker)
//            {
//                foreach (KeyValuePair<string, Money> addressMoney in collateral)
//                    this.depositsByAddress[addressMoney.Key] = addressMoney.Value;
//            }

//            return true;
//        }

//        public bool CheckCollateral(IServiceNode serviceNode)
//        {
//            if ((serviceNode.CollateralAmount == null) || (serviceNode.CollateralAmount == 0))
//            {
//                this.logger.LogTrace("(-)[NO_COLLATERAL_REQUIREMENT]:true");
//                return true;
//            }

//            lock (this.locker)
//            {
//                return this.depositsByAddress[serviceNode.CollateralMainchainAddress] >= serviceNode.CollateralAmount;
//            }
//        }

//        private void OnServiceNodeKicked(ServiceNodeRemoved serviceNodeRemoved)
//        {
//            lock (this.locker)
//            {
//                this.depositsByAddress.Remove(((ServiceNode)serviceNodeRemoved.RemovedNode).CollateralMainchainAddress);
//            }
//        }

//        private void OnServiceNodeAdded(ServiceNodeAdded serviceNodeAdded)
//        {
//            lock (this.locker)
//            {
//                this.depositsByAddress.Add(((ServiceNode)serviceNodeAdded.AddedNode).CollateralMainchainAddress, null);
//            }
//        }

//        public void Dispose()
//        {
//            this.signals.Unsubscribe(this.serviceNodeAddedToken);
//            this.signals.Unsubscribe(this.serviceNodeKickedToken);

//            this.cancellationSource.Cancel();

//            this.updateCollateralContinuouslyTask?.GetAwaiter().GetResult();
//        }
//    }
//}

        //private async Task PerformPeerCheckAsync()
        //{
        //    // TODO: remove own record
        //    var registrations = this.registrationStore.GetAll().ToList();

        //    var peerToCheck = registrations.OrderBy(a => RandomUtils.GetInt32()).FirstOrDefault();

        //    if (peerToCheck != null)
        //    {
        //        var nodeIpAddress = peerToCheck.Token.Ipv4Addr != null && peerToCheck.Token.Ipv4Addr != IPAddress.None
        //            ? peerToCheck.Token.Ipv4Addr
        //            : peerToCheck.Token.Ipv6Addr != null && peerToCheck.Token.Ipv6Addr != IPAddress.IPv6None
        //            ? peerToCheck.Token.Ipv6Addr
        //            : null;
        //        var nodePort = peerToCheck.Token.Port;

        //        if (nodeIpAddress != null)
        //        {
        //            // TODO: ping server ip and api ip, also get api base
        //            var peerEndpoint = new IPEndPoint(nodeIpAddress, nodePort);
        //            //var peer = await this.connectionManager.ConnectAsync(peerEndpoint).ConfigureAwait(false);
        //            //peer.IsConnected
        //        }
        //    }
        //}
