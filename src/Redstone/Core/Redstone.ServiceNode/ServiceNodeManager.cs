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

        /// <summary>
        /// Starts the service node manager.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the service node manager.
        /// <para>Internally it waits for async loops to complete before saving the wallets to disk.</para>
        /// </summary>
        void Stop();

        /// <summary>Provides up to date list of service nodes.</summary>
        /// <remarks>
        /// Blocks that are not signed with private keys that correspond
        /// to public keys from this list are considered to be invalid.
        /// </remarks>
        List<IServiceNode> GetServiceNodes();

        IServiceNode GetByServerId(string serverId);

        void AddServiceNode(IServiceNode serviceNodeMember);

        void RemoveServiceNode(IServiceNode serviceNodeMember);

        int SyncedHeight { get; set; }

        uint256 SyncedBlockHash { get; set; }
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

        public virtual void Start()
        {
            LoadServiceNodes();

            // TODO: what if load fails? Shouldn't we sync again

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
            if (this.ServiceNodes.All(x => x.EcdsaPubKey != this.CurrentServiceNodeKey.PubKey))
            {
                string message = "Key provided is not registered on the network!";

                this.Logger.LogWarning(message);
            }

            this.Logger.LogInformation("ServiceNode key pair was successfully loaded. Your public key is: '{0}'.", this.CurrentServiceNodeKey.PubKey);
        }
        public virtual void Stop()
        {
            this.SaveServiceNodes();
        }

        private void SetIsServiceNode()
        {
            this.IsServiceNode = this.ServiceNodes.Any(x => x.EcdsaPubKey == this.CurrentServiceNodeKey?.PubKey);
        }

        /// <inheritdoc />
        public List<IServiceNode> GetServiceNodes()
        {
            lock (this.locker)
            {
                return new List<IServiceNode>(this.ServiceNodes);
            }
        }

        public IServiceNode GetByServerId(string serverId)
        {
            lock (this.locker)
            {
                return this.ServiceNodes.FirstOrDefault(sn => sn.ServerId == serverId);
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

                // Remove any that have a matching collateral address
                BitcoinAddress serviceNodeCollateralAddress = serviceNode.GetCollateralAddress(this.network);
                var nodesWithMatchingPubKeys = this.ServiceNodes.Where(sn => sn.GetCollateralAddress(this.network) == serviceNodeCollateralAddress).ToList();
                foreach (IServiceNode matchingNode in nodesWithMatchingPubKeys)
                {
                    this.ServiceNodes.Remove(matchingNode);
                }

                // Add new one
                this.ServiceNodes.Add(serviceNode);

                SaveServiceNodes();
                SetIsServiceNode();

                this.Logger.LogInformation("Federation member '{0}' was added!", serviceNode);

                // Publish events
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

        public int SyncedHeight { get; set; }

        public uint256 SyncedBlockHash { get; set; }

        protected abstract void SaveServiceNodes();

        /// <summary>Loads saved collection of service nodes from the database.</summary>
        protected abstract void LoadServiceNodes();
    }

    public class ServiceNodeManager : ServiceNodeManagerBase
    {
        private const string SyncedHeightKey = "syncedheight";
        private const string SyncedBlockHashKey = "syncedblockhash";
        private const string ServiceNodesKey = "servicenodes";

        public ServiceNodeManager(NodeSettings nodeSettings, Network network, ILoggerFactory loggerFactory, IKeyValueRepository keyValueRepo, ISignals signals)
            : base(nodeSettings, network, loggerFactory, keyValueRepo, signals)
        {
        }

        protected override void SaveServiceNodes()
        {
            this.KeyValueRepo.SaveValueJson(SyncedHeightKey, this.SyncedHeight);
            this.KeyValueRepo.SaveValueJson(SyncedBlockHashKey, this.SyncedBlockHash);
            this.KeyValueRepo.SaveValueJson(ServiceNodesKey, GetServiceNodes());
        }

        /// <inheritdoc />
        protected override void LoadServiceNodes()
        {
            this.SyncedHeight = this.KeyValueRepo.LoadValueJson<int>(SyncedHeightKey);
            this.SyncedBlockHash = this.KeyValueRepo.LoadValueJson<uint256>(SyncedBlockHashKey);
            var loadedNodes = this.KeyValueRepo.LoadValueJson<List<Models.ServiceNode>>(ServiceNodesKey)?.ToList<IServiceNode>();
            if (loadedNodes != null)
            {
                SetServiceNodes(loadedNodes);
            }

            if (this.ServiceNodes == null)
            {
                this.Logger.LogTrace("(-)[NOT_FOUND]:null");
            }
        }
    }
}
