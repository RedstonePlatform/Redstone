namespace Redstone.Core.Networks
{
    using NBitcoin;
    using NBitcoin.Networks;

    public static class RedstoneNetworks
    {
        public static NetworksSelector NetworksSelector
        {
            get
            {
                return new NetworksSelector(() => Main, () => TestNet, () => RegTest);
            }
        }
        public static Network Main => NetworkRegistration.GetNetwork("RedstoneMain") ?? NetworkRegistration.Register(new RedstoneMain());

        public static Network TestNet => NetworkRegistration.GetNetwork("RedstoneTestNet") ?? NetworkRegistration.Register(new RedstoneTest());

        public static Network RegTest => NetworkRegistration.GetNetwork("RedstoneRegTest") ?? NetworkRegistration.Register(new RedstoneRegTest());
    }
}