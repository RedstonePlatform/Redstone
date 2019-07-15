using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Networks;
using NLog.Extensions.Logging;
using Redstone.Core.Networks;
using Stratis.Bitcoin.Builder.Feature;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Configuration.Logging;
using Stratis.Bitcoin.Configuration.Settings;
using Stratis.Bitcoin.Utilities;

namespace Redstone.Features.ServiceNode
{
    // TODO: cleanup
    public class ServiceNodeSettings
    {
        /// <summary>Version of the protocol the current implementation supports.</summary>
        public const ServiceNodeProtocolVersion SupportedProtocolVersion = ServiceNodeProtocolVersion.TESTNET_INITIAL;

        /// <summary>Factory to create instance logger.</summary>
        public ILoggerFactory LoggerFactory { get; private set; }

        /// <summary>Instance logger.</summary>
        public ILogger Logger { get; private set; }

        /// <summary>Configuration related to logging.</summary>
        public LogSettings Log { get; private set; }

        /// <summary>List of paths to important files and folders.</summary>
        public DataFolder DataFolder { get; private set; }

        /// <summary>Path to the data directory. This value is read-only and is set in the constructor's args.</summary>
        public string DataDir { get; private set; }

        /// <summary>Path to the configuration file. This value is read-only and is set in the constructor's args.</summary>
        public string ConfigurationFile { get; private set; }

        /// <summary>Combined command line arguments and configuration file settings.</summary>
        public TextFileConfiguration ConfigReader { get; private set; }

        /// <summary>Supported protocol version.</summary>
        public ServiceNodeProtocolVersion ProtocolVersion { get; private set; }

        /// <summary>Lowest supported protocol version.</summary>
        public ServiceNodeProtocolVersion? MinProtocolVersion { get; set; }

        /// <summary>Specification of the network the node runs on - regtest/testnet/mainnet.</summary>
        public Network Network { get; private set; }

        /// <summary>The node's user agent.</summary>
        public string Agent { get; private set; }

        public IPAddress Ipv4Address { get; set; }
        public IPAddress Ipv6Address { get; set; }
        public string OnionAddress { get; set; }
        public int Port { get; set; }

        public Money TxOutputValue { get; set; }
        public Money TxFeeValue { get; set; }

        public string CollateralAddress { get; set; }
        public string RewardAddress { get; set; }

        public string ServiceEndpoint { get; set; }

        public string RsaKeyFile { get; set; }

        public ServiceNodeSettings(NodeSettings nodeSettings)
        {
            Guard.NotNull(nodeSettings, nameof(nodeSettings));

            this.Logger = nodeSettings.LoggerFactory.CreateLogger(typeof(ServiceNodeSettings).FullName);

            // Get values from config
            this.LoadSettingsFromConfig(nodeSettings);
        }

        /// <summary>
        /// Loads the node settings from the application configuration.
        /// </summary>
        /// <param name="nodeSettings">Application configuration.</param>
        private void LoadSettingsFromConfig(NodeSettings nodeSettings)
        {
            TextFileConfiguration config = nodeSettings.ConfigReader;

            try
            {
                Network network = null;
                var networkString = config.GetOrDefault<string>("network", "testnet");
                switch (networkString)
                {
                    case "testnet":
                        network = RedstoneNetworks.TestNet;
                        break;
                    case "regtest":
                        network = RedstoneNetworks.RegTest;
                        break;
                    case "main":
                        network = RedstoneNetworks.Main;
                        break;
                }

                if (this.Network != null && this.Network != network)
                {
                    throw new Exception("ERROR: Network information in config file is invalid or incompatible with switch");
                }

                this.Network = network;

                if (IPAddress.TryParse(config.GetOrDefault<string>("servicenode.ipv4", null), out var ipv4Address))
                {
                    this.Ipv4Address = ipv4Address;
                }

                if (IPAddress.TryParse(config.GetOrDefault<string>("servicenode.ipv6", null), out var ipv6Address))
                {
                    this.Ipv6Address = ipv4Address;
                }

                this.OnionAddress = config.GetOrDefault<string>("servicenode.onion", null);
                if (this.OnionAddress?.Length > 16)
                {
                    this.OnionAddress = null;
                }

                //if (Ipv4Address == null && Ipv6Address == null && OnionAddress == null)
                //{
                //    throw new Exception("ERROR: No valid IP/onion addresses in configuration");
                //}

                this.Port = config.GetOrDefault<int>("servicenode.port", 37123);

                // Use user keyfile; default new key if invalid
                //string bitcoinNetwork;

                //if (this.Network == NBitcoin.Network.Main)
                //    bitcoinNetwork = "MainNet";
                //else if (this.Network == NBitcoin.Network.RegTest)
                //    bitcoinNetwork = "RegTest";
                //else // Network == Network.TestNet
                //    bitcoinNetwork = "TestNet";

                //if (datadir == null)
                //{
                //    // Create default directory for key files if it does not already exist
                //    Directory.CreateDirectory(Path.Combine(GetDefaultDataDir("NTumbleBitServer"), bitcoinNetwork));

                //    this.ServiceRsaKeyFile = configFile.GetOrDefault<string>("tumbler.rsakeyfile",
                //        Path.Combine(GetDefaultDataDir("NTumbleBitServer"), bitcoinNetwork, "Tumbler.pem"));
                //}
                //else
                //{
                //    Directory.CreateDirectory(Path.Combine(datadir, bitcoinNetwork));

                //    this.ServiceRsaKeyFile = configFile.GetOrDefault<string>("tumbler.rsakeyfile",
                //        Path.Combine(datadir, bitcoinNetwork, "Tumbler.pem"));
                //}

                //this.ServiceRsaKeyFile = BreezeConfigurationValidator.ValidateTumblerRsaKeyFile(
                //    this.ServiceRsaKeyFile,
                //    this.ServiceRsaKeyFile
                //);

                var collateralAddress = config.GetOrDefault<string>("servicenode.collateraladdress", null);

                if (!string.IsNullOrWhiteSpace(collateralAddress))
                {
                    try
                    {
                        new KeyId(collateralAddress);
                        this.CollateralAddress = collateralAddress;
                    }
                    catch (Exception e)
                    {
                        throw new Exception("ERROR: Collateral address not valid", e);
                    }
                }

                var rewardAddress = config.GetOrDefault<string>("servicenode.rewardeaddress", null);
                if (!string.IsNullOrWhiteSpace(rewardAddress))
                {
                    try
                    {
                        new KeyId(rewardAddress);
                        this.RewardAddress = rewardAddress;
                    }
                    catch (Exception e)
                    {
                        throw new Exception("ERROR: Reward address not valid", e);
                    }
                }

                var serviceEndpoint = config.GetOrDefault<string>("servicenode.serviceendpoint", null);
                if (!string.IsNullOrWhiteSpace(serviceEndpoint))
                {
                    try
                    {
                        new Uri(serviceEndpoint);
                        this.ServiceEndpoint = serviceEndpoint;
                    }
                    catch (Exception e)
                    {
                        throw new Exception("ERROR: Service endpoint not valid", e);
                    }
                }


                this.TxOutputValue = new Money(config.GetOrDefault<int>("servicenode.txoutputvalue", 7000), MoneyUnit.Satoshi);
                this.TxFeeValue = new Money(config.GetOrDefault<int>("servicenode.txfeevalue", 10000), MoneyUnit.Satoshi);
            }
            catch (Exception e)
            {
                throw new Exception("ERROR: Unable to read configuration. " + e);
            }
        }

        /// <summary>
        /// Displays command-line help.
        /// </summary>
        /// <param name="network">The network to extract values from.</param>
        public static void PrintHelp(Network network)
        {
            Guard.NotNull(network, nameof(network));

            var builder = new StringBuilder();

            builder.AppendLine($"-network=<network>                             Network - e.g. testnet or mainnet");
            builder.AppendLine($"-servicenode.ipv4=<ipv4 address>               IPv4 address of servicenode");
            builder.AppendLine($"-servicenode.ipv6=<ipv6 address>               IPv6 address of servicenode");
            builder.AppendLine($"-servicenode.onion=<onion address>             Onion address of servicenode");
            builder.AppendLine($"-servicenode.port=<port>                       Port of servicenode. Default - 37123");
            builder.AppendLine($"-servicenode.regtxoutputvalue=<value>          Value of each registration transaction output (in satoshi) default = 1000");
            builder.AppendLine($"-servicenode.regtxfeevalue=<value>             Value of registration transaction fee (in satoshi) default = 10000");
            builder.AppendLine($"-servicenode.rsakeyfile=<rsakeyfile>           RSA keyfile for token signing");
            builder.AppendLine($"-servicenode.ecdsapubkey=<pubkey>              PubKey for token signing");
            builder.AppendLine($"-servicenode.collateraladdress=<pubkeyhash>    PubKeyHash to collateral");
            builder.AppendLine($"-servicenode.rewardaddress=<pubkeyhash>        PubKeyHash for rewards");
            builder.AppendLine($"-servicenode.serviceendpoint=<url>     Url to Redstone Endpoint for service");

            NodeSettings.Default(network).Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            builder.AppendLine("####Redstone ServiceNode registration settings####");
            builder.AppendLine("#network=testnet");
            builder.AppendLine("#servicenode.ipv4=127.0.0.1");
            builder.AppendLine("#servicenode.ipv6=");
            builder.AppendLine("#servicenode.onion=");
            builder.AppendLine("#servicenode.port=37123");
            builder.AppendLine("# Value of each registration transaction output (in satoshi) default = 1000");
            builder.AppendLine("#servicenode.regtxoutputvalue=");
            builder.AppendLine("# Value of registration transaction fee (in satoshi) default = 10000");
            builder.AppendLine("#servicenode.regtxfeevalue=");
            builder.AppendLine("#servicenode.oasurl=");
            builder.AppendLine("#servicenode.rsakeyfile=");
            builder.AppendLine("#servicenode.ecdsapubkey=");
            builder.AppendLine("#servicenode.collateraladdress=");
            builder.AppendLine("#servicenode.rewardaddress=");
            builder.AppendLine("#servicenode.serviceendpoint=");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LoggerFactory.Dispose();
        }
    }
}