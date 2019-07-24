using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.ServiceNode;
using Redstone.ServiceNode.Models;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Builder.Feature;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Configuration.Logging;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Notifications;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Redstone.Features.ServiceNode
{
    public class ServiceNodeFeature : FullNodeFeature
    {
        private readonly ILogger logger;
        private readonly IServiceNodeManager serviceNodeManager;
        private readonly IServiceNodeRegistrationChecker serviceNodeRegistrationChecker;
        private readonly IServiceNodeCollateralChecker serviceNodeCollateralChecker;

        public ServiceNodeFeature(ILoggerFactory loggerFactory,
            NodeSettings nodeSettings,
            IServiceNodeManager serviceNodeManager,
            IServiceNodeRegistrationChecker serviceNodeRegistrationChecker,
            IServiceNodeCollateralChecker serviceNodeCollateralChecker,
            INodeStats nodeStats)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.serviceNodeManager = serviceNodeManager;
            this.serviceNodeRegistrationChecker = serviceNodeRegistrationChecker;
            this.serviceNodeCollateralChecker = serviceNodeCollateralChecker;

            nodeStats.RegisterStats(this.AddComponentStats, StatsType.Component);
            nodeStats.RegisterStats(this.AddInlineStats, StatsType.Inline, 800);
        }

        public override void Dispose()
        {
            this.serviceNodeManager.Stop();
        }

        private void AddInlineStats(StringBuilder benchLog)
        {
            if (this.serviceNodeManager != null)
            {
                int height = this.serviceNodeManager.SyncedHeight;
                uint256 hash = this.serviceNodeManager.SyncedBlockHash;

                benchLog.AppendLine("ServiceNode.Height: ".PadRight(LoggingConfiguration.ColumnLength + 1) +
                               height.ToString().PadRight(8) +
                               " ServiceNode.Hash: ".PadRight(LoggingConfiguration.ColumnLength - 1) + hash);
            }
        }

        private void AddComponentStats(StringBuilder benchLog)
        {
            if (this.serviceNodeManager != null)
            {
                benchLog.AppendLine();
                benchLog.AppendLine("======Service Nodes======");

                foreach (IServiceNode serviceNode in this.serviceNodeManager.GetServiceNodes())
                {
                    benchLog.AppendLine(($"{serviceNode.CollateralPubKeyHash}" + ",").PadRight(LoggingConfiguration.ColumnLength + 20)
                                   + (" Block Registered : " + serviceNode.RegistrationRecord.BlockReceived).PadRight(LoggingConfiguration.ColumnLength + 20)
                                   + " Endpoint: " + serviceNode.RegistrationRecord.Token.ServiceEndpoint);
                }
            }
        }

        /// <inheritdoc />
        public override async Task InitializeAsync()
        {
            this.logger.LogTrace("()");

            this.serviceNodeRegistrationChecker.Initialize();
            this.serviceNodeManager.Start();

            this.logger.LogTrace("(-)");
        }

        /// <summary>
        /// Prints command-line help.
        /// </summary>
        /// <param name="network">The network to extract values from.</param>
        public static void PrintHelp(Network network)
        {
            ServiceNodeSettings.PrintHelp(network);
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            ServiceNodeSettings.BuildDefaultConfigurationFile(builder, network);
        }
    }

    public static class RegistrationFeatureExtension
    {
        public static IFullNodeBuilder AddServiceNodeRegistration(this IFullNodeBuilder fullNodeBuilder)
        {
            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<ServiceNodeFeature>()
                    .DependOn<BlockStoreFeature>()
                    .DependOn<BlockNotificationFeature>()
                    .DependOn<TransactionNotificationFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<IServiceNodeManager, ServiceNodeManager>();
                        services.AddSingleton<IServiceNodeRegistrationChecker, ServiceNodeRegistrationChecker>();
                        services.AddSingleton<IServiceNodeCollateralChecker, ServiceNodeCollateralChecker>();
                        services.AddSingleton<ServiceNodeController>();
                        services.AddSingleton<ServiceNodeSettings>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}