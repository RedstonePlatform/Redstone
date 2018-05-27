using NBitcoin;
using Redstone.Core;

namespace Redstone.Sdk.Server.Services
{
    public class NetworkService : INetworkService
    {
        public void InitializeNetwork(bool testNet)
        {
            Network _ = testNet ? RedstoneNetwork.RedstoneTest : RedstoneNetwork.RedstoneMain;
        }
    }
}