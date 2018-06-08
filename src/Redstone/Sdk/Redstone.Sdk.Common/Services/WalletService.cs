using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Redstone.Sdk.Exceptions;
using Redstone.Sdk.Models;

namespace Redstone.Sdk.Services
{
    public class WalletService : IWalletService
    {
        private readonly HttpClient client = new HttpClient();

        public async Task<WalletBuildTransactionModel> BuildTransactionAsync(BuildTransactionRequest request)
        {
            this.client.DefaultRequestHeaders.Accept.Clear();
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var jsonRequest = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await this.client.PostAsync("http://localhost:38222/api/wallet/build-transaction", content);
            }
            catch (Exception e)
            {
                throw new WalletServiceException("Failed to connect to node, is it running.");
            }

            if (!response.IsSuccessStatusCode)
                throw new WalletServiceException($"Request produced error code {response.StatusCode} from the node, with reason {response.ReasonPhrase}.");

            if (response.Content == null)
                throw new WalletServiceException("Response from node contained no content.");

            var jsonResponse = await response.Content.ReadAsStringAsync();
    
            return JsonConvert.DeserializeObject<WalletBuildTransactionModel>(jsonResponse);
        }

        public async Task<WalletSendTransactionModel> SendTransactionAsync(SendTransactionRequest request)
        {
            this.client.DefaultRequestHeaders.Accept.Clear();
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var jsonRequest = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await this.client.PostAsync("http://localhost:38222/api/wallet/send-transaction", content);
            }
            catch (Exception e)
            {
                throw new WalletServiceException("Failed to connect to node, is it running.");
            }

            if (!response.IsSuccessStatusCode)
                throw new WalletServiceException($"Request produced error code {response.StatusCode} from the node, with reason {response.ReasonPhrase}.");

            if (response.Content == null)
                throw new WalletServiceException("Response from node contained no content.");

            var jsonResponse = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<WalletSendTransactionModel>(jsonResponse);
        }

        public async Task<TransactionModel> GetTransactionAsync(GetTransactionRequest request)
        {
            this.client.DefaultRequestHeaders.Accept.Clear();
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await this.client.GetAsync(
                    $"http://localhost:38222/api/RPC/callbyname?methodName=getrawtransaction&txid={request.TransactionId}&verbose=1");
            }
            catch (Exception e)
            {
                throw new WalletServiceException("Failed to connect to node, is it running.");
            }

            if (!response.IsSuccessStatusCode)
                throw new WalletServiceException($"Request produced error code {response.StatusCode} from the node, with reason {response.ReasonPhrase}.");

            if (response.Content == null)
                throw new WalletServiceException("Response from node contained no content.");

            var jsonResponse = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TransactionModel>(jsonResponse);
        }
    }
}