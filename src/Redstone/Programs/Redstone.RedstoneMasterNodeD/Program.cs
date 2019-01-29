using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Redstone.Features.ServiceNode;
using Stratis.Bitcoin.Builder.Feature;
using System;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using Redstone.Core.Networks;

namespace Redstone.RedstoneServiceNodeD
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Network network = args.Contains("-testnet")
                    ? RedstoneNetworks.TestNet
                    : args.Contains("-regnet")
                        ? RedstoneNetworks.RegTest
                        : RedstoneNetworks.Main;

                // TODO: can we leverage this to inject settings for API if required?
                var apiFeatureForSettings = new List<IFeatureRegistration>();
                //new ServiceNodeSettings(network).CreateDefaultConfigurationFile(apiFeatureForSettings);

                var serviceProvider = new ServiceCollection()
                    .AddLogging()
                    //.AddSingleton<IApiService, ApiService>() need to inject api service
                    .BuildServiceProvider();

                var logger = serviceProvider
                    .GetService<ILoggerFactory>()
                    .AddConsole(LogLevel.Debug)
                    .CreateLogger<Program>();

                //ServiceNodeSettings settings = new ServiceNodeSettings(network);

                logger.LogInformation("{Time} Pre-initialising server to obtain parameters for configuration", DateTime.Now);

                // Start API Service to get settings - seems a bit hacky (but was fom tumbler)
                //var apiService = serviceProvider.GetService<IApiService>();
                //apiService.Start(config, true);

                string configurationHash = null;//apiService.runtime.ClassicTumblerParameters.GetHash().ToString();
                string onionAddress = null;// = apiService.runtime.TorUri.Host.Substring(0, 16);
                RsaKey tumblerKey = null;// = apiService.runtime.TumblerKey;

                // Mustn't be occupying hidden service URL when the TumblerService is reinitialised
                //preTumblerConfig.runtime.TorConnection.Dispose();

                // No longer need this instance of the class
                //apiService = null;

                //string regStorePath = Path.Combine(settings.DataDir, "registrationHistory.json");

                //logger.LogInformation("{Time} Registration history path {Path}", DateTime.Now, regStorePath);
                //logger.LogInformation("{Time} Checking node registration", DateTime.Now);

                //ServiceNodeRegistration registration = new ServiceNodeRegistration();

                //if (!registration.IsRegistrationValid(settings, regStorePath, configurationHash, onionAddress, tumblerKey))
                //{
                //    logger.LogInformation("{Time} Creating or updating node registration", DateTime.Now);
                //    var regTx = registration.PerformRegistration(settings, regStorePath, configurationHash, onionAddress, tumblerKey);
                //    if (regTx != null)
                //    {
                //        logger.LogInformation("{Time} Submitted transaction {TxId} via RPC for broadcast", DateTime.Now, regTx.GetHash().ToString());
                //    }
                //    else
                //    {
                //        logger.LogInformation("{Time} Unable to broadcast transaction via RPC", DateTime.Now);
                //        Environment.Exit(0);
                //    }
                //}
                //else
                //{
                //    logger.LogInformation("{Time} Node registration has already been performed", DateTime.Now);
                //}

                logger.LogInformation("{Time} Starting API server", DateTime.Now);

                //var apiService = serviceProvider.GetService<IApiService>();
                //apiService.Start(config, false);

                /// OLD

                //    Network network = args.Contains("-testnet")
                //        ? RedstoneNetworks.TestNet
                //        : args.Contains("-regnet")
                //            ? RedstoneNetworks.RegTest
                //            : RedstoneNetworks.Main;

                //    var nodeSettings = new NodeSettings(network: network, protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, args: args)
                //    {
                //        MinProtocolVersion = ProtocolVersion.ALT_PROTOCOL_VERSION
                //    };

                //    var node = new FullNodeBuilder()
                //        .UseNodeSettings(nodeSettings)
                //        .UseBlockStore()
                //        .UsePosConsensus()
                //        .UseMempool()
                //        .UseWallet()
                //        .UseApi()
                //        .AddServiceNodeRegistration()
                //        .AddRPC()
                //        .Build();

                //    if (node != null)
                //    {
                //        await node.RunAsync();
                //    }
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the service node. Details: '{0}'", ex.Message);
            }
        }
    }
}
