using NBitcoin;
using NBitcoin.Networks;
using Redstone.Core.Networks;

namespace Redstone.Sdk.Server.Services
{
    public class NetworkService : INetworkService
    {
        public Network InitializeNetwork(bool testNet)
        {
            return testNet ? NetworkRegistration.Register(new RedstoneTest())
                    : NetworkRegistration.Register(new RedstoneMain());
        }
    }
}