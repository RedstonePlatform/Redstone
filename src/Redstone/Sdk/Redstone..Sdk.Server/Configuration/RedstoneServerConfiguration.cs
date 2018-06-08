using Microsoft.Extensions.DependencyInjection;
using Redstone.Sdk.Server.Filters;
using Redstone.Sdk.Server.Services;
using Redstone.Sdk.Services;

namespace Redstone.Sdk.Server.Configuration
{
    public static class RedstoneServerConfiguration
    {
        // TODO: perhaps configurable stuff in the method
        public static void AddRedstoneServer(this IServiceCollection services)
        {
            services.AddScoped<HexResourceFilter>();
            services.AddScoped<TokenResourceFilter>();

            services.AddTransient<ITokenService, TokenService>();
            services.AddTransient<IWalletService, WalletService>();
            services.AddTransient<INetworkService, NetworkService>();
            services.AddTransient<IRequestHeaderService, RequestHeaderService>();
        }
    }
}
