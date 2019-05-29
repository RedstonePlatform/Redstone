using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.Features.ServiceNode.Common;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Builder.Feature;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Notifications;
using Stratis.Bitcoin.Features.Wallet;
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
        private readonly RegistrationStore registrationStore;
        private readonly IWalletSyncManager walletSyncManager;
        private readonly IServiceNodeManager registrationManager;

        private readonly Network network;

        public ServiceNodeFeature(ILoggerFactory loggerFactory,
            NodeSettings nodeSettings,
            ServiceNodeManager registrationManager,
            RegistrationStore registrationStore,
            IWalletSyncManager walletSyncManager)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.registrationManager = registrationManager;
            this.registrationStore = registrationStore;
            this.network = nodeSettings.Network;
            this.walletSyncManager = walletSyncManager;
            this.registrationStore.SetStorePath(nodeSettings.DataDir);
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            this.logger.LogTrace("()");

            IList<RegistrationRecord> registrationRecords = this.registrationStore.GetAll();
            this.logger.LogInformation("Restored {0} service node registrations from the configuration file", registrationRecords.Count);

            // If there are no registrations then revert back to the block height of when the service nodes were set-up.
            if (registrationRecords.Count == 0)
                RevertRegistrations();
            else
                VerifyRegistrationStore(registrationRecords);

            this.registrationManager.Initialize();


            this.logger.LogTrace("(-)");

            return Task.CompletedTask;
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

        private void RevertRegistrations()
        {
            this.logger.LogTrace("()");

            // For RegTest, it is not clear that re-issuing a sync command will be beneficial. Generally you want to sync from genesis in that case.
            var syncHeight = this.network.Name == "RedstoneMain"
                ? SyncHeightMain
                : this.network.Name == "RedstoneTest"
                    ? SyncHeightTest
                    : SyncHeightRegTest;

            this.logger.LogInformation("No registrations have been found; Syncing from height {0} in order to get service node registrations", syncHeight);

            this.walletSyncManager.SyncFromHeight(syncHeight);

            this.logger.LogTrace("(-)");
        }

        private void VerifyRegistrationStore(IList<RegistrationRecord> list)
        {
            this.logger.LogTrace("()");

            this.logger.LogTrace("VerifyRegistrationStore");

            // Verify that the registration store is in a consistent state on start-up. The signatures of all the records need to be validated.
            foreach (var registrationRecord in list)
            {
                if (registrationRecord.Token.Validate(this.network)) continue;

                this.logger.LogTrace("Deleting invalid registration : {0}", registrationRecord.RecordGuid);

                this.registrationStore.Delete(registrationRecord.RecordGuid);
            }

            this.logger.LogTrace("(-)");
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
                        services.AddSingleton<RegistrationStore>();
                        services.AddSingleton<ServiceNodeManager>();
                        services.AddSingleton<ServiceNodeController>();
                        services.AddSingleton<ServiceNodeSettings>();
                        // Swap to using block store client when separating service node from full node
                        //services.AddSingleton<IHttpClientFactory, Stratis.Bitcoin.Controllers.HttpClientFactory>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}