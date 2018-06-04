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

            // try api without token
            Console.WriteLine("Press a key to call API (before payment)");
            Console.ReadKey();
            var result = await CallApiAsync(token);
            Console.WriteLine($"Result: {result}");


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

                }
            }

            Console.WriteLine($"Token: {token}");

            Console.WriteLine("Press a key to make to call API (after payment)");
            Console.ReadKey();
            result = await CallApiAsync(token);
            Console.WriteLine($"Result: {result}");

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
            try
            {
                return await HttpClientHelper.HttpGet("http://localhost:55888/v1/demo", new[] { ("Redstone", $"token {token}") });
            }
            catch(Exception e)
            {
                return e.Message;
            }            
        }
    }
}
