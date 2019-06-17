using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.ServiceNode.Events;
using Redstone.ServiceNode.Models;
using Redstone.ServiceNode.Utils;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Signals;
using Stratis.Bitcoin.Utilities;

namespace Redstone.ServiceNode
{
    public interface IServiceNodeManager
    {
        /// <summary><c>true</c> in case current node is a registered service node.</summary>
        bool IsServiceNode { get; }

        /// <summary>Current service node's private key. <c>null</c> if <see cref="IsServiceNode"/> is <c>false</c>.</summary>
        Key CurrentServiceNodeKey { get; }

        void Initialize();

        /// <summary>Provides up to date list of service nodes.</summary>
        /// <remarks>
        /// Blocks that are not signed with private keys that correspond
        /// to public keys from this list are considered to be invalid.
        /// </remarks>
        List<IServiceNode> GetServiceNodes();

        void AddServiceNode(IServiceNode serviceNodeMember);

        void RemoveServiceNode(IServiceNode serviceNodeMember);
    }

    public abstract class ServiceNodeManagerBase : IServiceNodeManager
    {
        /// <inheritdoc />
        public bool IsServiceNode { get; private set; }

        /// <inheritdoc />
        public Key CurrentServiceNodeKey { get; private set; }

        protected readonly IKeyValueRepository KeyValueRepo;

        protected readonly ILogger Logger;

        private readonly NodeSettings settings;

        private readonly Network network;

        private readonly ISignals signals;

        /// <summary>Key for accessing list of nodes from <see cref="IKeyValueRepository"/>.</summary>
        protected const string ServiceNodesKey = "servicenodes";

        /// <summary>Collection of all active service nodes.</summary>
        /// <remarks>All access should be protected by <see cref="locker"/>.</remarks>
        protected List<IServiceNode> ServiceNodes;

        /// <summary>Protects access to <see cref="ServiceNodes"/>.</summary>
        private readonly object locker;

        protected ServiceNodeManagerBase(NodeSettings nodeSettings, Network network, ILoggerFactory loggerFactory, IKeyValueRepository keyValueRepo, ISignals signals)
        {
            this.settings = Guard.NotNull(nodeSettings, nameof(nodeSettings));
            this.network = Guard.NotNull(network, nameof(network));
            this.KeyValueRepo = Guard.NotNull(keyValueRepo, nameof(keyValueRepo));
            this.signals = Guard.NotNull(signals, nameof(signals));

            this.Logger = loggerFactory.CreateLogger(GetType().FullName);
            this.locker = new object();
        }

        public virtual void Initialize()
        {
            LoadServiceNodes();

            // If there are no registrations then revert back to the block height of when the service nodes were set-up.
            //if (registrationRecords.Count == 0)
            //    this.RevertRegistrations();
            //else
            //    this.VerifyRegistrationStore(registrationRecords);

            if (this.ServiceNodes == null)
            {
                this.ServiceNodes = new List<IServiceNode>();
                SaveServiceNodes();
            }

            this.Logger.LogInformation("Network contains {0} service nodes. Their public keys are: {1}",
                this.ServiceNodes.Count, Environment.NewLine + string.Join(Environment.NewLine, this.ServiceNodes));

            // Load key.
            Key key = new KeyTool(this.settings.DataFolder).LoadPrivateKey();

            this.CurrentServiceNodeKey = key;
            SetIsServiceNode();

            if (this.CurrentServiceNodeKey == null)
            {
                this.Logger.LogTrace("(-)[NOT_SERVICE_NODE]");
                return;
            }

            // Loaded key has to be a key for current service node.
            if (this.ServiceNodes.All(x => x.SigningPubKey != this.CurrentServiceNodeKey.PubKey))
            {
                string message = "Key provided is not registered on the network!";

                this.Logger.LogWarning(message);
            }

            this.Logger.LogInformation("Federation key pair was successfully loaded. Your public key is: '{0}'.", this.CurrentServiceNodeKey.PubKey);
        }

        private void SetIsServiceNode()
        {
            this.IsServiceNode = this.ServiceNodes.Any(x => x.SigningPubKey == this.CurrentServiceNodeKey?.PubKey);
        }

        /// <inheritdoc />
        public List<IServiceNode> GetServiceNodes()
        {
            lock (this.locker)
            {
                return new List<IServiceNode>(this.ServiceNodes);
            }
        }

        protected void SetServiceNodes(List<IServiceNode> serviceNodesToSet)
        {
            lock (this.locker)
            {
                this.ServiceNodes = serviceNodesToSet;
            }
        }

        public void AddServiceNode(IServiceNode serviceNode)
        {
            lock (this.locker)
            {
                if (this.ServiceNodes.Contains(serviceNode))
                {
                    this.Logger.LogTrace("(-)[ALREADY_EXISTS]");
                    return;
                }

                // Remove any that have a matching pubkey
                IEnumerable<IServiceNode> nodesWithMatchingPubKeys = this.ServiceNodes.Where(s => s.SigningPubKey == serviceNode.SigningPubKey).ToList();

                foreach (IServiceNode node in nodesWithMatchingPubKeys)
                {
                    this.ServiceNodes.Remove(serviceNode);
                }

                this.ServiceNodes.Add(serviceNode);

                SaveServiceNodes();
                SetIsServiceNode();

                this.Logger.LogInformation("Federation member '{0}' was added!", serviceNode);

                foreach (IServiceNode node in nodesWithMatchingPubKeys)
                {
                    this.signals.Publish(new ServiceNodeRemoved(serviceNode));
                }

                this.signals.Publish(new ServiceNodeAdded(serviceNode));
            }
        }

        public void RemoveServiceNode(IServiceNode serviceNode)
        {
            lock (this.locker)
            {
                this.ServiceNodes.Remove(serviceNode);

                SaveServiceNodes();
                SetIsServiceNode();

                this.Logger.LogInformation("Federation member '{0}' was removed!", serviceNode);
                this.signals.Publish(new ServiceNodeRemoved(serviceNode));
            }
        }

        protected abstract void SaveServiceNodes();

        /// <summary>Loads saved collection of service nodes from the database.</summary>
        protected abstract void LoadServiceNodes();

        // Default initial sysc height
        //private const int SyncHeightMain = 0;
        //private const int SyncHeightTest = 0;
        //private const int SyncHeightRegTest = 0;

        //private void RevertRegistrations()
        //{
        //    this.logger.LogTrace("()");

        //    // For RegTest, it is not clear that re-issuing a sync command will be beneficial. Generally you want to sync from genesis in that case.
        //    int syncHeight = this.network.Name == "RedstoneMain"
        //        ? SyncHeightMain
        //        : this.network.Name == "RedstoneTest"
        //            ? SyncHeightTest
        //            : SyncHeightRegTest;

        //    this.logger.LogInformation("No registrations have been found; Syncing from height {0} in order to get service node registrations", syncHeight);

        //    this.walletSyncManager.SyncFromHeight(syncHeight);

        //    this.logger.LogTrace("(-)");
        //}

        //private void VerifyRegistrationStore(IEnumerable<RegistrationRecord> list)
        //{
        //    this.logger.LogTrace("()");

        //    this.logger.LogTrace("VerifyRegistrationStore");

        //    // Verify that the registration store is in a consistent state on start-up. The signatures of all the records need to be validated.
        //    foreach (RegistrationRecord registrationRecord in list)
        //    {
        //        if (registrationRecord.Token.Validate(this.network)) continue;

        //        this.logger.LogTrace("Deleting invalid registration : {0}", registrationRecord.RecordGuid);

        //        this.registrationStore.Delete(registrationRecord.RecordGuid);
        //    }

        //    this.logger.LogTrace("(-)");
        //}
    }

    public class ServiceNodeManager : ServiceNodeManagerBase
    {
        public ServiceNodeManager(NodeSettings nodeSettings, Network network, ILoggerFactory loggerFactory, IKeyValueRepository keyValueRepo, ISignals signals)
            : base(nodeSettings, network, loggerFactory, keyValueRepo, signals)
        {
        }

        protected override void SaveServiceNodes()
        {
            this.KeyValueRepo.SaveValueJson(ServiceNodesKey, GetServiceNodes());
        }

        /// <inheritdoc />
        protected override void LoadServiceNodes()
        {
            SetServiceNodes(this.KeyValueRepo.LoadValueJson<List<IServiceNode>>(ServiceNodesKey));

            if (this.ServiceNodes == null)
            {
                this.Logger.LogTrace("(-)[NOT_FOUND]:null");
            }
        }
    }
}
