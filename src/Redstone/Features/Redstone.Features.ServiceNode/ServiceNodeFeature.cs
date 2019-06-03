using System.Collections.Generic;
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
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Notifications;
using Stratis.Bitcoin.Features.Wallet.Interfaces;

namespace Redstone.Features.ServiceNode
{
    public class ServiceNodeFeature : FullNodeFeature
    {
        // Default initial sysc height
        private const int SyncHeightMain = 0;
        private const int SyncHeightTest = 0;
        private const int SyncHeightRegTest = 0;

        private readonly ILogger logger;
        private readonly IWalletSyncManager walletSyncManager;
        private readonly IServiceNodeRegistrationChecker serviceNodeRegistrationChecker;
        private readonly IServiceNodeManager serviceNodeManager;
        private readonly IServiceNodeCollateralChecker serviceNodeCollateralChecker;

        private readonly Network network;

        public ServiceNodeFeature(ILoggerFactory loggerFactory,
            NodeSettings nodeSettings,
            IWalletSyncManager walletSyncManager,
            IServiceNodeRegistrationChecker serviceNodeRegistrationChecker,
            IServiceNodeManager registrationManager,
            IServiceNodeCollateralChecker serviceNodeCollateralChecker)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.serviceNodeManager = registrationManager;
            this.serviceNodeRegistrationChecker = serviceNodeRegistrationChecker;
            this.serviceNodeCollateralChecker = serviceNodeCollateralChecker;
            this.network = nodeSettings.Network;
            this.walletSyncManager = walletSyncManager;
        }

        /// <inheritdoc />
        public override async Task InitializeAsync()
        {
            this.logger.LogTrace("()");

            this.serviceNodeRegistrationChecker.Initialize();
            this.serviceNodeManager.Initialize();

            //await this.serviceNodeCollateralChecker.InitializeAsync().ConfigureAwait(false);

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
                        services.AddSingleton<ServiceNodeManager>();
                        services.AddSingleton<ServiceNodeRegistrationChecker>();
                        services.AddSingleton<ServiceNodeCollateralChecker>();
                        services.AddSingleton<ServiceNodeController>();
                        services.AddSingleton<ServiceNodeSettings>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}