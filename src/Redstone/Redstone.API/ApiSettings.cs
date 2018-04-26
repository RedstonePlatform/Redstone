using System;
using NBitcoin;
using Stratis.Bitcoin.Utilities;

namespace Redstone.API
{
    public class ApiSettings : Stratis.Bitcoin.Features.Api.ApiSettings
    {
        public const int DefaultRedstoneApiPort = 37222;
        /// <summary>The default port used by the API when the node runs on the AppCoin testnet network.</summary>
        public const int TestRedstoneApiPort = 38222;

        public ApiSettings(Action<Stratis.Bitcoin.Features.Api.ApiSettings> callback) : base(callback)
        {
        }

        protected override int GetDefaultPort(Network network)
        {
            return network.IsTest() ? TestRedstoneApiPort : DefaultRedstoneApiPort;
        }
    }
}
