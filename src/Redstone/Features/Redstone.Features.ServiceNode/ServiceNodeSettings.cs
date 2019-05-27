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

        public string RpcUser { get; set; }
        public string RpcPassword { get; set; }
        public string RpcUrl { get; set; }

        public IPAddress Ipv4Address { get; set; }
        public IPAddress Ipv6Address { get; set; }
        public string OnionAddress { get; set; }
        public int Port { get; set; }

        public Money TxOutputValue { get; set; }
        public Money TxFeeValue { get; set; }

        public string ServiceUrl { get; set; }

        public string ServiceApiBaseUrl { get; set; }
        public string ServiceRsaKeyFile { get; set; }
        public string ServiceEcdsaKeyAddress { get; set; }

        public ServiceNodeSettings(NodeSettings nodeSettings)
        {
            Guard.NotNull(nodeSettings, nameof(nodeSettings));

            this.Logger = nodeSettings.LoggerFactory.CreateLogger(typeof(ServiceNodeSettings).FullName);

            // Get values from config
            this.LoadSettingsFromConfig(nodeSettings);

            // Check validity of settings
            this.CheckConfigurationValidity(nodeSettings.Logger);
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

                this.RpcUser = config.GetOrDefault<string>("rpc.user", null);
                this.RpcPassword = config.GetOrDefault<string>("rpc.password", null);
                this.RpcUrl = config.GetOrDefault<string>("rpc.url", null);

                //if (this.RpcUser == null || this.RpcPassword == null || this.RpcUrl == null)
                //{
                //    throw new Exception("ERROR: RPC information in config file is invalid");
                //}

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

                this.ServiceApiBaseUrl = config.GetOrDefault<string>("tumbler.url", null);

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

                this.ServiceEcdsaKeyAddress = config.GetOrDefault<string>("service.ecdsakeyaddress", null);

                this.TxOutputValue = new Money(config.GetOrDefault<int>("servicenode.txoutputvalue", 7000), MoneyUnit.Satoshi);
                this.TxFeeValue = new Money(config.GetOrDefault<int>("servicenode.txfeevalue", 10000), MoneyUnit.Satoshi);
            }
            catch (Exception e)
            {
                throw new Exception("ERROR: Unable to read configuration. " + e);
            }
        }

        /// <summary>
        /// Checks the validity of the RPC settings or forces them to be valid.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        private void CheckConfigurationValidity(ILogger logger)
        {
            // TODO: SN complete this - also do we need rpc any more?

            // Check that the settings are valid or force them to be valid
            // (Note that these values will not be set if server = false in the config)
            if (this.RpcPassword == null && this.RpcUser != null)
                throw new ConfigurationException("rpcpassword should be provided");
            if (this.RpcUser == null && this.RpcPassword != null)
                throw new ConfigurationException("rpcuser should be provided");

            // We can now safely assume that server was set to true in the config or that the
            // "AddRpc" callback provided a user and password implying that the Rpc feature will be used.
            if (this.RpcPassword != null && this.RpcUser != null)
            {
                // this.Server = true;

                // If the "Bind" list has not been specified via callback..
                //if (this.Bind.Count == 0)
                //    this.Bind = this.DefaultBindings;

                //if (this.AllowIp.Count == 0)
                //{
                //    if (this.Bind.Count > 0)
                //        logger.LogWarning("WARNING: RPC bind selection (-rpcbind) was ignored because allowed ip's (-rpcallowip) were not specified, refusing to allow everyone to connect");

                //    this.Bind.Clear();
                //    this.Bind.Add(new IPEndPoint(IPAddress.Parse("::1"), this.RPCPort));
                //    this.Bind.Add(new IPEndPoint(IPAddress.Parse("127.0.0.1"), this.RPCPort));
                //}

                //if (this.Bind.Count == 0)
                //{
                //    this.Bind.Add(new IPEndPoint(IPAddress.Parse("::"), this.RPCPort));
                //    this.Bind.Add(new IPEndPoint(IPAddress.Parse("0.0.0.0"), this.RPCPort));
                //}
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

            builder.AppendLine($"-network=<network>                         Network - e.g. testnet or mainnet");
            builder.AppendLine($"-servicenode.ipv4=<ipv4 address>           IPv4 address of servicenode");
            builder.AppendLine($"-servicenode.ipv6=<ipv6 address>           IPv6 address of servicenode");
            builder.AppendLine($"-servicenode.onion=<onion address>         Onion address of servicenode");
            builder.AppendLine($"-servicenode.port=<port>                   Port of servicenode. Default - 37123");
            builder.AppendLine($"-servicenode.regtxoutputvalue=<value>      Value of each registration transaction output (in satoshi) default = 1000");
            builder.AppendLine($"-servicenode.regtxfeevalue=<value>         Value of registration transaction fee (in satoshi) default = 10000");
            builder.AppendLine($"-service.url=<url>");
            builder.AppendLine($"-service.rsakeyfile=<rsakeyfile>");
            builder.AppendLine($"-service.ecdsakeyaddress=<key address>");

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
            builder.AppendLine("#service.url=");
            builder.AppendLine("#service.rsakeyfile=");
            builder.AppendLine("#service.ecdsakeyaddress=");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LoggerFactory.Dispose();
        }
    }
}