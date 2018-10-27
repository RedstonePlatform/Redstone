using NBitcoin.BouncyCastle.Security;

namespace NBitcoin
{
    internal class NBitcoinSecureRandom : SecureRandom
    {
        private static readonly NBitcoinSecureRandom _Instance = new NBitcoinSecureRandom();

        public static NBitcoinSecureRandom Instance => _Instance;

        /// <inheritdoc />
        private NBitcoinSecureRandom()
        {

        }

        public override void NextBytes(byte[] buffer)
        {
            RandomUtils.GetBytes(buffer);
        }

    }
}
