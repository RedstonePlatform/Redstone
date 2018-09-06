using NBitcoin;
using NBitcoin.Networks;

namespace Redstone.Core.Networks
{
    public static class RedstoneNetworks
    {
        public static Network Main => NetworkRegistration.GetNetwork("RedstoneMain") ?? NetworkRegistration.Register(new RedstoneMain());

        public static Network TestNet => NetworkRegistration.GetNetwork("RedstoneTestNet") ?? NetworkRegistration.Register(new RedstoneTest());

        public static Network RegTest => NetworkRegistration.GetNetwork("RedstoneRegTest") ?? NetworkRegistration.Register(new RedstoneRegTest());
    }
}