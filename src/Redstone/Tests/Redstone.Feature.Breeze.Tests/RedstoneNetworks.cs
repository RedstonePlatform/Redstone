using NBitcoin;
using NBitcoin.Networks;
using Redstone.Core.Networks;

namespace Redstone.Feature.Breeze.Tests
{
    public static class RedstoneNetworks
    {
        public static Network RedstoneMain => NetworkRegistration.GetNetwork("RedstoneMain") ?? NetworkRegistration.Register(new RedstoneMain());

        public static Network RedstoneTest => NetworkRegistration.GetNetwork("RedstoneTest") ?? NetworkRegistration.Register(new RedstoneTest());

        public static Network RedstoneRegTest => NetworkRegistration.GetNetwork("RedstoneRegTest") ?? NetworkRegistration.Register(new RedstoneRegTest());
    }
}