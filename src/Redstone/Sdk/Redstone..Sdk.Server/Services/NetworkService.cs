using NBitcoin;

namespace Redstone.Sdk.Server.Services
{
    public class NetworkService : INetworkService
    {
        public Network InitializeNetwork(bool testNet)
        {
            return testNet ? Network.RedstoneTest : Network.RedstoneMain;
        }
    }
}