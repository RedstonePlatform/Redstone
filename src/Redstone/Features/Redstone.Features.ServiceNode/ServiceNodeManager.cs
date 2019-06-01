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

namespace Redstone.Features.ServiceNode
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

        protected readonly IKeyValueRepository keyValueRepo;

        protected readonly ILogger logger;

        private readonly NodeSettings settings;

        private readonly Network network;

        private readonly ISignals signals;

        /// <summary>Key for accessing list of nodes from <see cref="IKeyValueRepository"/>.</summary>
        protected const string serviceNodesKey = "servicenodes";

        /// <summary>Collection of all active service nodes.</summary>
        /// <remarks>All access should be protected by <see cref="locker"/>.</remarks>
        protected List<IServiceNode> serviceNodes;

        /// <summary>Protects access to <see cref="serviceNodes"/>.</summary>
        private readonly object locker;

        public ServiceNodeManagerBase(NodeSettings nodeSettings, Network network, ILoggerFactory loggerFactory, IKeyValueRepository keyValueRepo, ISignals signals)
        {
            this.settings = Guard.NotNull(nodeSettings, nameof(nodeSettings));
            this.network = Guard.NotNull(network, nameof(network));
            this.keyValueRepo = Guard.NotNull(keyValueRepo, nameof(keyValueRepo));
            this.signals = Guard.NotNull(signals, nameof(signals));

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.locker = new object();
        }

        public virtual void Initialize()
        {
            LoadServiceNodes();

            if (this.serviceNodes == null)
            {
                this.serviceNodes = new List<IServiceNode>();
                this.SaveServiceNodes();
            }

            this.logger.LogInformation("Network contains {0} service nodes. Their public keys are: {1}",
                this.serviceNodes.Count, Environment.NewLine + string.Join(Environment.NewLine, this.serviceNodes));

            // Load key.
            Key key = new KeyTool(this.settings.DataFolder).LoadPrivateKey();

            this.CurrentServiceNodeKey = key;
            this.SetIsServiceNode();

            if (this.CurrentServiceNodeKey == null)
            {
                this.logger.LogTrace("(-)[NOT_FED_MEMBER]");
                return;
            }

            // Loaded key has to be a key for current service node.
            if (!this.serviceNodes.Any(x => x.PubKey == this.CurrentServiceNodeKey.PubKey))
            {
                string message = "Key provided is not registered on the network!";

                this.logger.LogWarning(message);
            }

            this.logger.LogInformation("Federation key pair was successfully loaded. Your public key is: '{0}'.", this.CurrentServiceNodeKey.PubKey);
        }

        private void SetIsServiceNode()
        {
            this.IsServiceNode = this.serviceNodes.Any(x => x.PubKey == this.CurrentServiceNodeKey?.PubKey);
        }

        /// <inheritdoc />
        public List<IServiceNode> GetServiceNodes()
        {
            lock (this.locker)
            {
                return new List<IServiceNode>(this.serviceNodes);
            }
        }

        public void AddServiceNode(IServiceNode serviceNode)
        {
            lock (this.locker)
            {
                if (this.serviceNodes.Contains(serviceNode))
                {
                    this.logger.LogTrace("(-)[ALREADY_EXISTS]");
                    return;
                }

                // Remove any that have a matching pubkey
                IEnumerable<IServiceNode> nodesWithMatchingPubKeys = this.serviceNodes.Where(s => s.PubKey == serviceNode.PubKey);

                foreach (IServiceNode node in nodesWithMatchingPubKeys)
                {
                    this.serviceNodes.Remove(serviceNode);
                }

                this.serviceNodes.Add(serviceNode);

                this.SaveServiceNodes();
                this.SetIsServiceNode();

                this.logger.LogInformation("Federation member '{0}' was added!", serviceNode);

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
                this.serviceNodes.Remove(serviceNode);

                this.SaveServiceNodes();
                this.SetIsServiceNode();

                this.logger.LogInformation("Federation member '{0}' was removed!", serviceNode);
                this.signals.Publish(new ServiceNodeRemoved(serviceNode));
            }
        }

        protected abstract void SaveServiceNodes();

        /// <summary>Loads saved collection of service nodes from the database.</summary>
        protected abstract void LoadServiceNodes();
    }

    public class ServiceNodeManager : ServiceNodeManagerBase
    {
        public ServiceNodeManager(NodeSettings nodeSettings, Network network, ILoggerFactory loggerFactory, IKeyValueRepository keyValueRepo, ISignals signals)
            : base(nodeSettings, network, loggerFactory, keyValueRepo, signals)
        {
        }

        protected override void SaveServiceNodes()
        {
            this.keyValueRepo.SaveValueJson(serviceNodesKey, this.serviceNodes);
        }

        /// <inheritdoc />
        protected override void LoadServiceNodes()
        {
            this.serviceNodes = this.keyValueRepo.LoadValueJson<List<IServiceNode>>(serviceNodesKey);

            if (this.serviceNodes == null)
            {
                this.logger.LogTrace("(-)[NOT_FOUND]:null");
            }
        }
    }
}
