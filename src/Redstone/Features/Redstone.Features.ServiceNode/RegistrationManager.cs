using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.ServiceNode;
using Redstone.ServiceNode.Events;
using Redstone.ServiceNode.Models;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.EventBus;
using Stratis.Bitcoin.EventBus.CoreEvents;
using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.BlockStore.AddressIndexing;
using Stratis.Bitcoin.Features.BlockStore.Controllers;
using Stratis.Bitcoin.Signals;

namespace Redstone.Features.ServiceNode
{
    public class RegistrationManager : IRegistrationManager
    {
        public static readonly int MAX_PROTOCOL_VERSION = 128; // >128 = regard as test versions
        public static readonly int MIN_PROTOCOL_VERSION = 1;

        private readonly IRegistrationStore registrationStore;
        private readonly Network network;
        private readonly ISignals signals;
        private readonly ILogger logger;
        public readonly IAddressIndexer addressIndexer;
        private SubscriptionToken nodeAddedSubscription;
        private SubscriptionToken nodeRemovedSubscription;

        public RegistrationManager(
            ILoggerFactory loggerFactory,
            NodeSettings nodeSettings,
            IRegistrationStore registrationStore,
            ISignals signals,
            IAddressIndexer addressIndexer)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.registrationStore = registrationStore;
            this.network = nodeSettings.Network;
            this.signals = signals;

            this.addressIndexer = addressIndexer;
            this.logger.LogInformation("Initialized RegistrationManager");
        }

        public void Initialize()
        {
            this.nodeAddedSubscription = this.signals.Subscribe<ServiceNodeAdded>(this.OnNodeAdded);
            this.nodeRemovedSubscription = this.signals.Subscribe<ServiceNodeRemoved>(this.OnNodeRemoved);
        }

        private void OnNodeRemoved(ServiceNodeRemoved nodeRemoved)
        {
            IServiceNode serviceNode = nodeRemoved.RemovedNode;
            this.registrationStore.DeleteAllForServer(serviceNode.RegistrationRecord.Token.ServerId);
        }

        private void OnNodeAdded(ServiceNodeAdded nodeAdded)
        {
            IServiceNode serviceNode = nodeAdded.AddedNode;
            this.registrationStore.AddWithReplace(serviceNode.RegistrationRecord);
        }

        public void Dispose()
        {
            this.signals.Unsubscribe(this.nodeRemovedSubscription);
            this.signals.Unsubscribe(this.nodeAddedSubscription);
        }
    }
}
