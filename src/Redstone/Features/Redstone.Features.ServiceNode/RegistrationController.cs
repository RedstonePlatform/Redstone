using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.Features.ServiceNode.Models;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Connection;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Broadcasting;
using Stratis.Bitcoin.Features.Wallet.Controllers;
using Stratis.Bitcoin.Features.Wallet.Helpers;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Features.Wallet.Models;
using Stratis.Bitcoin.Utilities;
using Stratis.Bitcoin.Utilities.JsonErrors;
using Stratis.Bitcoin.Utilities.ModelStateErrors;

namespace Redstone.Features.ServiceNode
{
    /// <summary>
    /// Controller providing operations on a service node.
    /// </summary>
    [Route("api/[controller]")]
    public class RegistrationController : Controller
    {
        private readonly IWalletManager walletManager;

        private readonly IWalletTransactionHandler walletTransactionHandler;

        private readonly IWalletSyncManager walletSyncManager;

        private readonly CoinType coinType;

        /// <summary>Specification of the network the node runs on - regtest/testnet/mainnet.</summary>
        private readonly Network network;

        private readonly IConnectionManager connectionManager;

        private readonly ConcurrentChain chain;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        private readonly IBroadcasterManager broadcasterManager;

        /// <summary>Provider of date time functionality.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        private readonly NodeSettings nodeSettings;

        private readonly ServiceNodeSettings serviceNodeSettings;

        public RegistrationController(
            ILoggerFactory loggerFactory,
            IWalletManager walletManager,
            IWalletTransactionHandler walletTransactionHandler,
            IWalletSyncManager walletSyncManager,
            IConnectionManager connectionManager,
            Network network,
            ConcurrentChain chain,
            IBroadcasterManager broadcasterManager,
            IDateTimeProvider dateTimeProvider,
            NodeSettings nodeSettings,
            ServiceNodeSettings serviceNodeSettings)
        {
            this.walletManager = walletManager;
            this.walletTransactionHandler = walletTransactionHandler;
            this.walletSyncManager = walletSyncManager;
            this.connectionManager = connectionManager;
            this.network = network;
            this.coinType = (CoinType)network.Consensus.CoinType;
            this.chain = chain;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.broadcasterManager = broadcasterManager;
            this.dateTimeProvider = dateTimeProvider;
            this.nodeSettings = nodeSettings;
            this.serviceNodeSettings = serviceNodeSettings;
        }

        [Route("registerservicenode")]
        [HttpPost]
        public async Task<IActionResult> RegisterServiceNodeAsync(RegisterServiceNodeRequest request)
        {
            Key key;

            try
            {
                key = this.GetPrivateKey(request.WalletName, request.Password);
            }
            catch (FileNotFoundException e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.NotFound, "This wallet was not found at the specified location.", e.ToString());
            }
            catch (SecurityException e)
            {
                // indicates that the password is wrong
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.Forbidden, "Wrong password, please try again.", e.ToString());
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }

            try
            {
                // TODO: SN inject?
                var registration = new ServiceNodeRegistration(this.network, 
                    this.nodeSettings, 
                    this.broadcasterManager, 
                    this.walletTransactionHandler);

                // TODO: work out what is right - pass key into endpoint or grab from config (or support both)
                var ecdsa = new BitcoinSecret(key, this.network);

                var config = new ServiceNodeRegistrationConfig
                {
                    ProtocolVersion = (int)ServiceNodeProtocolVersion.INITIAL,
                    Ipv4Address = this.serviceNodeSettings.Ipv4Address ?? IPAddress.None,
                    Ipv6Address = this.serviceNodeSettings.Ipv6Address ?? IPAddress.IPv6None,
                    OnionAddress = null,
                    Port = this.serviceNodeSettings.Port,
                    ConfigurationHash = "0123456789012345678901234567890123456789", // TODO hash of config file
                    EcdsaPubKey = ecdsa.PubKey,
                    TxFeeValue = this.serviceNodeSettings.TxFeeValue,
                    TxOutputValue = this.serviceNodeSettings.TxOutputValue
                };

                // TODO: SN this needs to be loaded from pem file
                var rsa = new RsaKey();

                if (!registration.IsRegistrationValid(config))
                {
                    logger.LogInformation("{Time} Creating or updating node registration", DateTime.Now);
                    Transaction regTx = await registration.PerformRegistrationAsync(config, request.WalletName, request.Password, request.AccountName, ecdsa, rsa);
                    if (regTx != null)
                    {
                        logger.LogInformation("{Time} Submitted node registration transaction {TxId} for broadcast", DateTime.Now, regTx.GetHash().ToString());
                    }
                    else
                    {
                        logger.LogInformation("{Time} Unable to broadcast transaction", DateTime.Now);
                        Environment.Exit(0);
                    }

                    return Ok(new { txHash = regTx.GetHash() });
                }
                else
                {
                    logger.LogInformation("{Time} Node registration has already been performed", DateTime.Now);
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Node registration failed", "Registration has already been performed successfully, you can't do it again.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private Key GetPrivateKey(string walletName, string walletPassword)
        {
            Wallet wallet = this.walletManager.LoadWallet(walletPassword, walletName);
            Key key = HdOperations.DecryptSeed(wallet.EncryptedSeed, walletPassword, this.network);
            return key;
            //var hdAddress = wallet.GetAllAddressesByCoinType(CoinType.Redstone).First(hda => hda.Address == address);
            //var privateKey = wallet.GetExtendedPrivateKeyForAddress(walletPassword, hdAddress);
            //return privateKey;
        }
    }
}
