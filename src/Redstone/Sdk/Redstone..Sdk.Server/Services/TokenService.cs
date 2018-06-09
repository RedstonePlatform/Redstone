using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NBitcoin;
using Redstone.Sdk.Models;
using Redstone.Sdk.Server.Configuration;
using Redstone.Sdk.Server.Exceptions;
using Redstone.Sdk.Server.Utils;
using Redstone.Sdk.Services;

namespace Redstone.Sdk.Server.Services
{
    // TODO:
    // move httpcontextaccessor part to a different service
    public class TokenService : ITokenService
    {
        private readonly Network _network;
        private readonly RedstoneServerOptions _options;
        private readonly IPaymentPolicy _paymentPolicy;
        private readonly IWalletService _walletService;
        private readonly IRequestHeaderService _requestHeaderService;

        public TokenService(IOptions<RedstoneServerOptions> options, IPaymentPolicy paymentPolicy, INetworkService networkService, IWalletService walletService, IRequestHeaderService requestHeaderService)
        {
            _options = options.Value;
            _paymentPolicy = paymentPolicy;
            _network = networkService.InitializeNetwork(true);
            _walletService = walletService;
            _requestHeaderService = requestHeaderService;
        }

        public string GetHex()
        {
            return this._requestHeaderService.GetRedstoneHeader(RedstoneContants.RedstoneHexScheme);
        }

        // TODO coin units?
        public bool ValidateHex()
        {
            try
            {
                var transaction = Transaction.Load(GetHex(), _network);

                // TODO any other checks
                return _paymentPolicy.IsPaymentValid(transaction.TotalOut);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> AcceptHex()
        {
            // TODO: break this out to be more specific
            if (!ValidateHex())
                throw new TokenServiceException("Hex not valid or payment too low");

            WalletSendTransactionModel transactionModel;

            try
            {
                transactionModel = await this._walletService.SendTransactionAsync(new SendTransactionRequest
                {
                    Hex = GetHex()
                });
            }
            catch (Exception e)
            {
                throw new TokenServiceException("Failed to send transaction", e);
            }

            // We've taken payment now, shouldn't really allow an exception to be thrown. How can we ensure token
            // is generated without error.
            try
            {
                return EncryptDecrypt.EncryptString(transactionModel.TransactionId, _options.PrivateKey);
            }
            catch (Exception e)
            {
                throw new TokenServiceException("Failed to generate token from transaction", e);
            }
        }

        public string GetToken()
        {
            return this._requestHeaderService.GetRedstoneHeader(RedstoneContants.RedstoneTokenScheme);
        }

        public async Task<bool> ValidateTokenAsync()
        {
            // TODO: better error handling
            try
            {
                var token = GetToken();

                if (string.IsNullOrEmpty(token))
                    return false;

                var transactionId = EncryptDecrypt.DecryptString(GetToken(), _options.PrivateKey);

                var transaction = await this._walletService.GetTransactionAsync(new GetTransactionRequest {TransactionId = transactionId});
                
                return _paymentPolicy.IsTransactionValid(transaction);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}