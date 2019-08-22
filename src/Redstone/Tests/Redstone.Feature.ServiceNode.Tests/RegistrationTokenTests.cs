using System;
using System.Net;
using System.Text;
using NBitcoin;
using Redstone.Core.Networks;
using Redstone.Features.ServiceNode;
using Redstone.ServiceNode.Models;
using Redstone.ServiceNode.Utils;
using Xunit;

namespace Redstone.Feature.ServiceNode.Tests
{
    public class RegistrationTokenTests
    {
        [Fact]
         public void CanValidateRegistrationToken()
        {
            var privateKey = new Key();

            var token = new RegistrationToken(
                (int)ServiceNodeProtocolVersion.INITIAL,
                IPAddress.Parse("127.0.0.1"),
                IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
                "",
                37123,
                new KeyId("dbb476190a81120928763ee8ce97e4c0bcfd6624"),
                new KeyId("dbb476190a81120928763ee8ce97e4c0bcfd6624"),
                privateKey.PubKey,
                new Uri("https://restone.com/servicetest"));

            token.Signature = CryptoUtils.SignData(token.GetHeaderBytes().ToArray(), privateKey);

            Assert.True(token.Validate(RedstoneNetworks.Main));
        }

        
        [Fact]
        public void CheckSignatureOfRegistrationToken()
        {
            var privateKey = new Key();

            var token = new RegistrationToken(1,
                IPAddress.Parse("172.16.1.10"),
                IPAddress.Parse("2001:0db8:85a3:0000:1234:8a2e:0370:7334"),
                "",
                16174,
                new KeyId("dbb476190a81120928763ee8ce97e4c0bcfd6624"),
                new KeyId("dbb476190a81120928763ee8ce97e4c0bcfd6624"),
                privateKey.PubKey,
                new Uri("https://redstone.com/test"));

            // Only the 'header' portion of the registration token gets signed, minus the length bytes
            var message = token.GetHeaderBytes();

            token.Signature = CryptoUtils.SignData(message.ToArray(), privateKey);

            var signature = CryptoUtils.SignData(message.ToArray(), privateKey);
            Assert.True(CryptoUtils.VerifySignature(message.ToArray(), privateKey.PubKey, Encoding.UTF8.GetString(signature)));
            Assert.True(token.VerifySignatures());
        }

        [Fact]
        public void CanVerifySignature()
        {
            var privateKey = new Key();

            var token = new RegistrationToken(1,
                IPAddress.Parse("172.16.1.10"),
                IPAddress.Parse("2001:0db8:85a3:0000:1234:8a2e:0370:7334"),
                "",
                16174,
                new KeyId("dbb476190a81120928763ee8ce97e4c0bcfd6624"),
                new KeyId("dbb476190a81120928763ee8ce97e4c0bcfd6624"),
                privateKey.PubKey,
                new Uri("https://redstone.com.test"));

            token.Signature = CryptoUtils.SignData(token.GetHeaderBytes().ToArray(), privateKey);

            Assert.True(token.VerifySignatures());
        }
    }
}