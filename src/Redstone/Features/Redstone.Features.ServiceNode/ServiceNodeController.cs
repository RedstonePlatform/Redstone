using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Redstone.ServiceNode;
using Redstone.ServiceNode.Models;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Connection;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Utilities;
using Stratis.Bitcoin.Utilities.JsonErrors;

namespace Redstone.Features.ServiceNode
{
    /// <summary>
    /// Controller providing operations on a service node.
    /// </summary>
    [Route("api/[controller]")]
    public class ServiceNodeController : Controller
    {
        private readonly IWalletManager walletManager;

        private readonly IWalletTransactionHandler walletTransactionHandler;

        /// <summary>Specification of the network the node runs on - regtest/testnet/mainnet.</summary>
        private readonly Network network;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        private readonly IBroadcasterManager broadcasterManager;

        private readonly IServiceNodeManager serviceNodeManager;

        private readonly NodeSettings nodeSettings;

        private readonly ServiceNodeSettings serviceNodeSettings;

        public ServiceNodeController(
            ILoggerFactory loggerFactory,
            IWalletManager walletManager,
            IWalletTransactionHandler walletTransactionHandler,
            Network network,
            IBroadcasterManager broadcasterManager,
            IServiceNodeManager serviceNodeManager,
            NodeSettings nodeSettings,
            ServiceNodeSettings serviceNodeSettings)
        {
            this.walletManager = walletManager;
            this.walletTransactionHandler = walletTransactionHandler;
            this.network = network;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.broadcasterManager = broadcasterManager;
            this.serviceNodeManager = serviceNodeManager;
            this.nodeSettings = nodeSettings;
            this.serviceNodeSettings = serviceNodeSettings;
        }

        [HttpGet("registrations")]
        public IActionResult GetRegistrations()
        {
            try
            {
                List<IServiceNode> serviceNodes = this.serviceNodeManager.GetServiceNodes();
                IEnumerable<RegistrationModel> models = serviceNodes.Select(m => new RegistrationModel
                {
                    ServerId = m.RegistrationRecord.Token.ServerId,
                    BlockReceived = m.RegistrationRecord.BlockReceived,
                    RecordTimestamp = m.RegistrationRecord.RecordTimestamp,
                    RecordTxHex = m.RegistrationRecord.RecordTxHex,
                    RecordTxId = m.RegistrationRecord.RecordTxId
                });
                return this.Json(models);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        [Route("register")]
        [HttpPost]
        [ProducesResponseType(typeof(RegisterServiceNodeResponse), 200)]
        public async Task<IActionResult> RegisterAsync(RegisterServiceNodeRequest request)
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
                    this.walletTransactionHandler,
                    this.walletManager);

                (KeyId collateralPubKeyHash, KeyId rewardPubKeyHash) = GetPubKeyHashes(this.serviceNodeSettings, request.WalletName, request.AccountName);

                var config = new ServiceNodeRegistrationConfig
                {
                    ProtocolVersion = (int)ServiceNodeProtocolVersion.INITIAL,
                    Ipv4Address = this.serviceNodeSettings.Ipv4Address ?? IPAddress.None,
                    Ipv6Address = this.serviceNodeSettings.Ipv6Address ?? IPAddress.IPv6None,
                    OnionAddress = null,
                    Port = this.serviceNodeSettings.Port,
                    ConfigurationHash = "0123456789012345678901234567890123456789", // TODO hash of config file
                    EcdsaPrivateKey = key.GetBitcoinSecret(this.network),
                    CollateralPubKeyHash = collateralPubKeyHash,
                    RewardPubKeyHash = rewardPubKeyHash,
                    TxFeeValue = this.serviceNodeSettings.TxFeeValue,
                    TxOutputValue = this.serviceNodeSettings.TxOutputValue
                };

                // TODO: SN this needs to be loaded from pem file
                var rsa = new RsaKey();

                if (!registration.IsRegistrationValid(config))
                {
                    logger.LogInformation("{Time} Creating or updating node registration", DateTime.Now);
                    Transaction regTx = await registration.PerformRegistrationAsync(config, request.WalletName, request.Password, request.AccountName, rsa);
                    if (regTx != null)
                    {
                        logger.LogInformation("{Time} Submitted node registration transaction {TxId} for broadcast", DateTime.Now, regTx.GetHash().ToString());
                    }
                    else
                    {
                        logger.LogInformation("{Time} Unable to broadcast transaction", DateTime.Now);
                        Environment.Exit(0);
                    }

                    return Ok(new RegisterServiceNodeResponse
                    {
                        CollateralPubKeyHash = collateralPubKeyHash.ToString(),
                        RewardPubKeyHash = rewardPubKeyHash.ToString(),
                        RegistrationTxHash = regTx.GetHash().ToString()
                    });
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
            HdAddress hdAddress = wallet.GetAllAddresses().FirstOrDefault();
            if (hdAddress == null)
                throw new InvalidOperationException("Could not find adreess in specified wallet");
            ISecret extendedPrivateKey = wallet.GetExtendedPrivateKeyForAddress(walletPassword, hdAddress);
            return extendedPrivateKey.PrivateKey;
        }

        private (KeyId collateralPubKeyHash, KeyId rewardPubKeyHash) GetPubKeyHashes(ServiceNodeSettings serviceNodeSettings, string walletName, string accountName)
        {
            var unusedAddresses = this.walletManager.GetUnusedAddresses(new WalletAccountReference(walletName, accountName), 2).ToList();
            var collateralAddress = serviceNodeSettings.CollateralAddress ?? unusedAddresses[0].ToString();
            var rewardAddress = serviceNodeSettings.RewardAddress ?? unusedAddresses[1].ToString();

            KeyId collateralPubKeyHash = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(BitcoinAddress.Create(collateralAddress, this.network).ScriptPubKey);
            KeyId rewardPubKeyHash = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(BitcoinAddress.Create(rewardAddress, this.network).ScriptPubKey);
            return (collateralPubKeyHash, rewardPubKeyHash);
        }
    }
}
