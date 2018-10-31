using Redstone.Core.Networks;

namespace Redstone.Feature.Breeze.Tests
{
    using System.Collections.Generic;
    using System.Text;
    using NBitcoin;
    using Redstone.Features.Breeze.BreezeCommon;
    using Xunit;

    public class ConversionTests
    {
        [Fact]
        public void ConvertPubKeyTest()
        {
            byte[] input = Encoding.ASCII.GetBytes("abc");

            List<PubKey> keys = BlockChainDataConversions.BytesToPubKeys(input);
            
            byte[] converted = BlockChainDataConversions.PubKeysToBytes(keys);

            Assert.Equal(0x61, converted[0]);
            Assert.Equal(0x62, converted[1]);
            Assert.Equal(0x63, converted[2]);
        }

        [Fact]
        public void BytesToAddresses_ShortMessage()
        {            
            // a - mpMqqfKnF9M2rwk9Ai4RymBqADx6TssFuM <- correct version on testnet
            //     mpMqqfKnF9M2rwk9Ai4RymBqADx6TUnBkb <- incorrect version with 00000000 checksum bytes

            string inputMessage = "a"; 
            byte[] inputMessageBytes = Encoding.ASCII.GetBytes(inputMessage);
            List<BitcoinAddress> output = BlockChainDataConversions.BytesToAddresses(RedstoneNetworks.TestNet, inputMessageBytes);
            List<BitcoinAddress> expectedOutput = new List<BitcoinAddress>();
            expectedOutput.Add(BitcoinAddress.Create("mpMqqfKnF9M2rwk9Ai4RymBqADx6TssFuM"));

            /* Worked example
            // 0x00 - Mainnet
            // 0x6F - Testnet
            // 0x?? - Stratis mainnet

            // Literal 'a' is 0x61 hex

            var keyBytes = new byte[] {0x6F, 0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
            var algorithm = SHA256.Create();
            var hash = algorithm.ComputeHash(algorithm.ComputeHash(keyBytes));

            First 4 bytes of double SHA256: 15, 146, 165, 196
            Need to concatenate them to keyBytes

            var keyBytes2 = new byte[] {0x6F,
                0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                15, 146, 165, 196};

            var finalEncodedAddress = Encoders.Base58.EncodeData(keyBytes2);

            Result should be "mpMqqfKnF9M2rwk9Ai4RymBqADx6TssFuM"
            */

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void AddressesToBytes_ShortMessage()
        {
            byte[] expectedBytes = new byte[] {
                0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            };

            List<BitcoinAddress> inputAddresses = new List<BitcoinAddress>();
            inputAddresses.Add(BitcoinAddress.Create("mpMqqfKnF9M2rwk9Ai4RymBqADx6TssFuM"));

            byte[] output = BlockChainDataConversions.AddressesToBytes(inputAddresses);

            Assert.Equal(expectedBytes, output);
        }
    }
}