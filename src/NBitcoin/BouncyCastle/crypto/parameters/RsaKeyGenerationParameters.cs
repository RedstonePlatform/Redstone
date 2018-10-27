using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Security;

namespace NBitcoin.BouncyCastle.crypto.parameters
{
	internal class RsaKeyGenerationParameters
		: KeyGenerationParameters
	{
		private readonly BigInteger publicExponent;
		private readonly int certainty;

		public RsaKeyGenerationParameters(
			BigInteger publicExponent,
			SecureRandom random,
			int strength,
			int certainty)
			: base(random, strength)
		{
			this.publicExponent = publicExponent;
			this.certainty = certainty;
		}

		public BigInteger PublicExponent
		{
			get
			{
				return this.publicExponent;
			}
		}

		public int Certainty
		{
			get
			{
				return this.certainty;
			}
		}

		public override bool Equals(
			object obj)
		{
			RsaKeyGenerationParameters other = obj as RsaKeyGenerationParameters;

			if(other == null)
			{
				return false;
			}

			return this.certainty == other.certainty
				&& this.publicExponent.Equals(other.publicExponent);
		}

		public override int GetHashCode()
		{
			return this.certainty.GetHashCode() ^ this.publicExponent.GetHashCode();
		}
	}
}