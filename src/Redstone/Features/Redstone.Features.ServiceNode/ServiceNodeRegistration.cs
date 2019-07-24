using System;
using System.Threading.Tasks;
using NBitcoin;
using Redstone.ServiceNode;
using Redstone.ServiceNode.Models;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Wallet.Interfaces;

namespace Redstone.Features.ServiceNode
{
    public enum ServiceNodeProtocolVersion
    {
        INITIAL = 1,
        TESTNET_INITIAL = 253,
    }

    public class ServiceNodeRegistration
    {
        private readonly Network network;

        private readonly IWalletTransactionHandler walletTransactionHandler;

        private readonly IWalletManager walletManager;

        private readonly IBroadcasterManager broadcasterManager;

        private readonly IServiceNodeManager serviceNodeManager;

        public ServiceNodeRegistration(Network network,
            NodeSettings nodeSettings,
            IBroadcasterManager broadcasterManager,
            IWalletTransactionHandler walletTransactionHandler,
            IWalletManager walletManager,
            IServiceNodeManager serviceNodeManager)
        {
            this.network = network;
            this.broadcasterManager = broadcasterManager;
            this.walletTransactionHandler = walletTransactionHandler;
            this.walletManager = walletManager;
            this.serviceNodeManager = serviceNodeManager;
        }

        public bool IsRegistrationValid(IServiceNodeRegistrationConfig registrationConfig)
        {
            IServiceNode serviceNode = this.serviceNodeManager.GetByServerId(registrationConfig.CollateralPubKeyHash.ToString());

            // If no transactions exist, the registration definitely needs to be done
            if (serviceNode == null)
                return false;

            RegistrationToken registrationToken = serviceNode.RegistrationRecord.Token;

            // IPv4
            if (registrationConfig.Ipv4Address == null && registrationToken.Ipv4Addr != null)
                return false;

            if (registrationConfig.Ipv4Address != null && registrationToken.Ipv4Addr == null)
                return false;

            if (registrationConfig.Ipv4Address != null
                && registrationToken.Ipv4Addr != null
                && !registrationConfig.Ipv4Address.Equals(registrationToken.Ipv4Addr))
                return false;

            // IPv6
            if (registrationConfig.Ipv6Address == null && registrationToken.Ipv6Addr != null)
                return false;

            if (registrationConfig.Ipv6Address != null && registrationToken.Ipv6Addr == null)
                return false;

            if (registrationConfig.Ipv6Address != null
                && registrationToken.Ipv6Addr != null
                && !registrationConfig.Ipv6Address.Equals(registrationToken.Ipv6Addr))
                return false;

            // Onion
            if (registrationConfig.OnionAddress != registrationToken.OnionAddress)
                return false;

            if (registrationConfig.Port != registrationToken.Port)
                return false;

            if (registrationConfig.CollateralPubKeyHash != registrationToken.CollateralPubKeyHash)
                return false;

            if (registrationConfig.RewardPubKeyHash != registrationToken.RewardPubKeyHash)
                return false;

            if (registrationConfig.ServiceEndpoint != registrationToken.ServiceEndpoint)
                return false;

            // TODO: Check if transaction is actually confirmed on the blockchain?

            return true;
        }

        public async Task<Transaction> PerformRegistrationAsync(IServiceNodeRegistrationConfig registrationConfig,
            string walletName, string walletPassword, string accountName, RsaKey serviceRsaKey)
        {
            Transaction transaction = null;
            try
            {
                RegistrationToken registrationToken = registrationConfig.CreateRegistrationToken(this.network);

                transaction = TransactionUtils.BuildTransaction(this.network,
                    this.walletTransactionHandler,
                    this.walletManager,
                    registrationConfig,
                    registrationToken,
                    walletName,
                    accountName,
                    walletPassword,
                    serviceRsaKey);

                await this.broadcasterManager.BroadcastTransactionAsync(transaction).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Unable to broadcast registration transaction");
                Console.WriteLine(e);
            }

            return transaction;
        }
    }
}
