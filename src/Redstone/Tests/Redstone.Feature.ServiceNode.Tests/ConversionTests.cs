using System.Collections.Generic;
using System.Text;
using NBitcoin;
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
        public void AddressesToBytes_ShortMessage()
        {
            byte[] expectedBytes = new byte[] {
                0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            };

            var inputAddresses = new List<BitcoinAddress> {BitcoinAddress.Create("TJp6YfcXar2jF1LA3QjmfzfeD26hp9qXNn", RedstoneNetworks.TestNet)};

            byte[] output = BlockChainDataConversions.AddressesToBytes(inputAddresses);

            Assert.Equal(expectedBytes, output);
        }

        [Fact]
        public void BytesToAddresses_ShortMessage()
        {            
            // a - TJp6YfcXar2jF1LA3QjmfzfeD26hp9qXNn <- correct version on redstone testnet

            string inputMessage = "a"; 
            byte[] inputMessageBytes = Encoding.ASCII.GetBytes(inputMessage);
            List<BitcoinAddress> output = BlockChainDataConversions.BytesToAddresses(RedstoneNetworks.TestNet, inputMessageBytes);

            var expectedOutput = new List<BitcoinAddress> {BitcoinAddress.Create("TJp6YfcXar2jF1LA3QjmfzfeD26hp9qXNn")};

            /* Worked example
            // 0x00 - Bitcoin Mainnet
            // 0x6F - Bitcoin Testnet
            // 0x41 - Redstone test <----
            // 0x3c - Redstone main

            // Literal 'a' is 0x61 hex
            var keyBytes = new byte[] {0x41, 0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
            var algorithm = SHA256.Create();
            var hash = algorithm.ComputeHash(algorithm.ComputeHash(keyBytes));

            //First 4 bytes of double SHA256: 15, 146, 165, 196
            //Need to concatenate them to keyBytes

            var keyBytes2 = new byte[] {0x41,
                0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                134, 212, 83, 7};

            var finalEncodedAddress = Encoders.Base58.EncodeData(keyBytes2);
            /*
            Result should be "TJp6YfcXar2jF1LA3QjmfzfeD26hp9qXNn"
            */

            Assert.Equal(expectedOutput, output);
        }
	}
}