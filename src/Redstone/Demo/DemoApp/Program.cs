using System;
using System.Threading.Tasks;
using Redstone.Sdk.Models;
using Redstone.Sdk.Services;
using Newtonsoft.Json;

namespace Redstone.DemoApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {
            var token = "";
            var result = "";
            // try api without token
            Console.WriteLine("Press a key to call API (before payment)");
            Console.ReadKey();

            try
            {
                result = await CallApiAsync(token);
                Console.WriteLine($"Result: {result}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed : {e.Message}");
            }

            var transaction = await BuildTransactionAsync(args);
            while (token == "")
            {
                try
                {
                    Console.WriteLine("Press a key to make a payment");
                    Console.ReadKey();

                    var tokenResponse = await GetApiTokenAsync(transaction.Hex);
                    token = JsonConvert.DeserializeAnonymousType(tokenResponse, new { token = "" }).token;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed : {e.Message}");
                }
            }

            Console.WriteLine($"Token: {token}");

            while (result == "")
            {
                try
                {
                    Console.WriteLine("Press a key to make to call API (after payment)");
                    Console.ReadKey();

                    result = await CallApiAsync(token);
                    Console.WriteLine($"Result: {result}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed : {e.Message}");
                }
            }

            Console.WriteLine("Press a key to quit");
            Console.ReadKey();
        }

        private static async Task<WalletBuildTransactionModel> BuildTransactionAsync(string[] args)
        {
            var walletService = new WalletService();
            var transaction = await walletService.BuildTransactionAsync(new BuildTransactionRequest
            {
                AccountName = "account 0",
                Amount = "1",
                AllowUnconfirmed = true,
                DestinationAddress = "TLX2UxwiqoANX34f91Y3CCRD8atySdBsBL",
                FeeAmount = "0.005",
                FeeType = "low",
                Password = "redstone1",
                WalletName = "redstone1",
                ShuffleOutputs = true,
            });

            Console.WriteLine($"Here is your hex: {transaction.Hex}");
            return transaction;
        }

        public static async Task<string> GetApiTokenAsync(string hex)
        {
            return await HttpClientHelper.HttpGet("http://localhost:55888/v1/token", new[] { ("Redstone", $"hex {hex}") });
        }

        public static async Task<string> CallApiAsync(string token)
        {
            return await HttpClientHelper.HttpGet("http://localhost:55888/v1/demo", new[] { ("Redstone", $"token {token}") });
        }
    }
}
