using System.Text;
using NBitcoin;

namespace Redstone.Features.MasterNode.Common
{
    public class CryptoUtils
    {
        RsaKey TumblerRsaKey;
        BitcoinSecret EcdsaKey;

        public CryptoUtils(RsaKey rsaKey, BitcoinSecret privateKeyEcdsa)
        {
            this.TumblerRsaKey = rsaKey;
            this.EcdsaKey = privateKeyEcdsa;
        }

        public byte[] SignDataRSA(byte[] message)
        {
            byte[] signedBytes;
            NBitcoin.uint160 temp1;

            signedBytes = this.TumblerRsaKey.Sign(message, out temp1);

            return signedBytes;
        }

        public byte[] SignDataECDSA(byte[] message)
        {
            var signature = this.EcdsaKey.PrivateKey.SignMessage(message);
            var signedBytes = Encoding.UTF8.GetBytes(signature);

            return signedBytes;
        }

        public static bool VerifySignatureECDSA(byte[] message, PubKey key, string signature)
        {
            return key.VerifyMessage(message, signature);
        }
    }
}