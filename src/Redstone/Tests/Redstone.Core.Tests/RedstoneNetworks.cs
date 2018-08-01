using NBitcoin;
using NBitcoin.Networks;
using Redstone.Core.Networks;

namespace Redstone.Core.Tests
{
    public static class RedstoneNetworks
    {
        public static Network RedstoneMain => NetworkRegistration.GetNetwork("Main") ?? NetworkRegistration.Register(new RedstoneMain());

        public static Network RedstoneTest => NetworkRegistration.GetNetwork("TestNet") ?? NetworkRegistration.Register(new RedstoneTest());

        public static Network RedstoneRegTest => NetworkRegistration.GetNetwork("RegTest") ?? NetworkRegistration.Register(new RedstoneRegTest());
    }
}