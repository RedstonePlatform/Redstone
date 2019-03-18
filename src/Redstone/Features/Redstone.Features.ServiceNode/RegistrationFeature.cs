using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.Core.Networks;
using Redstone.Features.ServiceNode.Common;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Builder.Feature;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Notifications;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Features.WatchOnlyWallet;
using Stratis.Bitcoin.Signals;

namespace Redstone.Features.ServiceNode
{
    public class RegistrationFeature : FullNodeFeature
    {
        // Default initial sysc height
        private const int SyncHeightMain = 0;
        private const int SyncHeightTest = 0;
        private const int SyncHeightRegTest = 0;

        private readonly ILogger logger;
        private readonly RegistrationStore registrationStore;
        private readonly IWalletSyncManager walletSyncManager;

        private readonly ILoggerFactory loggerFactory;
        private readonly IRegistrationManager registrationManager;

        private readonly Network network;

        public RegistrationFeature(ILoggerFactory loggerFactory,
            NodeSettings nodeSettings,
            RegistrationManager registrationManager,
            RegistrationStore registrationStore,
            ISignals signals,
            IWatchOnlyWalletManager watchOnlyWalletManager,
            IWalletSyncManager walletSyncManager)
        {
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.registrationManager = registrationManager;
            this.registrationStore = registrationStore;
            this.network = nodeSettings.Network;
            this.walletSyncManager = walletSyncManager;
            this.registrationStore.SetStorePath(nodeSettings.DataDir);
        }

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

            this.logger.LogTrace("(-)");

            return Task.CompletedTask;
        }

        private void RevertRegistrations()
        {
            this.logger.LogTrace("()");

            // For RegTest, it is not clear that re-issuing a sync command will be beneficial. Generally you want to sync from genesis in that case.
            var syncHeight = this.network == RedstoneNetworks.Main 
                ? SyncHeightMain
                : this.network == RedstoneNetworks.TestNet 
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
                if (registrationRecord.Record.Validate(this.network)) continue;

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
                    .AddFeature<RegistrationFeature>()
                    .DependOn<BlockNotificationFeature>()
                    .DependOn<TransactionNotificationFeature>()
                    .DependOn<WalletFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<RegistrationStore>();
                        services.AddSingleton<RegistrationManager>();
                        services.AddSingleton<ServiceNodeController>();
                        services.AddSingleton<ServiceNodeSettings>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}