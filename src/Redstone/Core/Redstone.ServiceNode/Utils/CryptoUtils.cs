using System.Text;
using NBitcoin;

namespace Redstone.ServiceNode.Utils
{
    public class CryptoUtils
    {
        public static byte[] SignData(byte[] message, Key privateKey)
        {
            var signature = privateKey.SignMessage(message);
            var signedBytes = Encoding.UTF8.GetBytes(signature);

            return signedBytes;
        }

        public static bool VerifySignature(byte[] message, PubKey key, string signature)
        {
            return key.VerifyMessage(message, signature);
        }
    }
}