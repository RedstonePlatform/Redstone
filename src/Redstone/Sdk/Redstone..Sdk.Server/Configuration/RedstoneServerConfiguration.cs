using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Redstone.Sdk.Server.Filters;
using Redstone.Sdk.Server.Services;
using Redstone.Sdk.Services;

namespace Redstone.Sdk.Server.Configuration
{
    public static class RedstoneServerConfiguration
    {
        public static RedstoneServiceBuilder AddRedstoneServer(this IServiceCollection services, Action<IRedstoneServerOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);

            services.AddSingleton<IRedstoneServerOptions, RedstoneServerOptions>();

            services.AddScoped<HexResourceFilter>();
            services.AddScoped<TokenResourceFilter>();

            services.AddTransient<ITokenService, TokenService>();
            services.AddTransient<IWalletService, WalletService>();
            services.AddTransient<INetworkService, NetworkService>();
            services.AddTransient<IRequestHeaderService, RequestHeaderService>();

            return new RedstoneServiceBuilder(services);
        }
    }

    public class RedstoneServiceBuilder
    {
        public IServiceCollection Services { get; }

        public RedstoneServiceBuilder(IServiceCollection services)
        {
            this.Services = services;
        }

        public virtual RedstoneServiceBuilder AddDefaultPaymentPolicy()
        {
            this.Services.AddTransient<IPaymentPolicy, DefaultPaymentPolicy>();
            return this;

        }

        public virtual RedstoneServiceBuilder AddPaymentPolicy<TPaymentPolicy>() where TPaymentPolicy : class, IPaymentPolicy
        {
            this.Services.AddTransient<IPaymentPolicy, TPaymentPolicy>();
            return this;
        }
    }
}
