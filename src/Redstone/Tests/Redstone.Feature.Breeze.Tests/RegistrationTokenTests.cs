using System.Net;
using NBitcoin;
using NBitcoin.Networks;
using Redstone.Core.Networks;
using Redstone.Features.Breeze.BreezeCommon;
using Xunit;

namespace Redstone.Feature.Breeze.Tests
{
	public class RegistrationTokenTests
	{
		[Fact]
		public void CanValidateRegistrationToken()
		{
			var rsa = new RsaKey();
			var ecdsa = new Key().GetBitcoinSecret(RedstoneNetworks.Main);

			var serverAddress = ecdsa.GetAddress().ToString();
			
			var token = new RegistrationToken(255,
				serverAddress,
				IPAddress.Parse("127.0.0.1"),
				IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
				"0123456789ABCDEF",
				"",
				37123,
				ecdsa.PubKey);

			var cryptoUtils = new CryptoUtils(rsa, ecdsa);
			token.RsaSignature = cryptoUtils.SignDataRSA(token.GetHeaderBytes().ToArray());
			token.EcdsaSignature = cryptoUtils.SignDataECDSA(token.GetHeaderBytes().ToArray());

			Assert.True(token.Validate(RedstoneNetworks.Main));
		}
	}
}