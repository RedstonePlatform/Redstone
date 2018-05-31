using NBitcoin;

namespace Redstone.Sdk.Server.Services
{
    public interface INetworkService
    {
        Network InitializeNetwork(bool testNet);
    }
}