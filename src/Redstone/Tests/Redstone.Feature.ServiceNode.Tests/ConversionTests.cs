using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using NBitcoin;
using NBitcoin.DataEncoders;
using Redstone.Core.Networks;
using Redstone.Features.ServiceNode.Common;
using Xunit;

namespace Redstone.Feature.ServiceNode.Tests
{
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
            List<BitcoinAddress> output = BlockChainDataConversions.BytesToAddresses(Stratis.Bitcoin.Networks.Networks.Stratis.Testnet(), inputMessageBytes);
            //List<BitcoinAddress> output = BlockChainDataConversions.BytesToAddresses(Stratis.Bitcoin.Networks.Networks.Bitcoin.Testnet(), inputMessageBytes);
            List<BitcoinAddress> expectedOutput = new List<BitcoinAddress>();
            //expectedOutput.Add(BitcoinAddress.Create("SW8taT1xAV6yc93yza58hk84x1apRXiHrv"));
            expectedOutput.Add(BitcoinAddress.Create("mpMqqfKnF9M2rwk9Ai4RymBqADx6TssFuM"));
            // Worked example
            // 0x00 - Mainnet
            // 0x63 - Testnet

            // Literal 'a' is 0x61 hex

            var keyBytes = new byte[] {0x63, 0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
            var algorithm = SHA256.Create();
            var hash = algorithm.ComputeHash(algorithm.ComputeHash(keyBytes));

            //First 4 bytes of double SHA256: 90, 242, 0, 148
            //Need to concatenate them to keyBytes

            var keyBytes2 = new byte[] {0x63,
                0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                90, 242, 0, 148};

            var finalEncodedAddress = Encoders.Base58.EncodeData(keyBytes2);

            //Result should be "SW8taT1xAV6yc93yza58hk84x1apRXiHrv"
            //

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