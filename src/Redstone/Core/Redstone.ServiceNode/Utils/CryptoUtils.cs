using System.Text;
using NBitcoin;

namespace Redstone.ServiceNode.Utils
{
    public class CryptoUtils
    {
        public static byte[] SignDataECDSA(byte[] message, BitcoinSecret privateEcdsaKey)
        {
            var signature = privateEcdsaKey.PrivateKey.SignMessage(message);
            var signedBytes = Encoding.UTF8.GetBytes(signature);

            return signedBytes;
        }

        public static bool VerifySignatureECDSA(byte[] message, PubKey key, string signature)
        {
            return key.VerifyMessage(message, signature);
        }
    }
}