using System;
using NBitcoin.BouncyCastle.Math;

namespace NBitcoin.BouncyCastle.crypto.parameters
{
	internal class RsaPrivateCrtKeyParameters
		: RsaKeyParameters
	{
		private readonly BigInteger e, p, q, dP, dQ, qInv;

		public RsaPrivateCrtKeyParameters(
			BigInteger modulus,
			BigInteger publicExponent,
			BigInteger privateExponent,
			BigInteger p,
			BigInteger q,
			BigInteger dP,
			BigInteger dQ,
			BigInteger qInv)
			: base(true, modulus, privateExponent)
		{
			ValidateValue(publicExponent, "publicExponent", "exponent");
			ValidateValue(p, "p", "P value");
			ValidateValue(q, "q", "Q value");
			ValidateValue(dP, "dP", "DP value");
			ValidateValue(dQ, "dQ", "DQ value");
			ValidateValue(qInv, "qInv", "InverseQ value");

			this.e = publicExponent;
			this.p = p;
			this.q = q;
			this.dP = dP;
			this.dQ = dQ;
			this.qInv = qInv;
		}

		public BigInteger PublicExponent
		{
			get
			{
				return this.e;
			}
		}

		public BigInteger P
		{
			get
			{
				return this.p;
			}
		}

		public BigInteger Q
		{
			get
			{
				return this.q;
			}
		}

		public BigInteger DP
		{
			get
			{
				return this.dP;
			}
		}

		public BigInteger DQ
		{
			get
			{
				return this.dQ;
			}
		}

		public BigInteger QInv
		{
			get
			{
				return this.qInv;
			}
		}

		public override bool Equals(
			object obj)
		{
			if(obj == this)
				return true;

			RsaPrivateCrtKeyParameters kp = obj as RsaPrivateCrtKeyParameters;

			if(kp == null)
				return false;

			return kp.DP.Equals(this.dP)
				&& kp.DQ.Equals(this.dQ)
				&& kp.Exponent.Equals(this.Exponent)
				&& kp.Modulus.Equals(this.Modulus)
				&& kp.P.Equals(this.p)
				&& kp.Q.Equals(this.q)
				&& kp.PublicExponent.Equals(this.e)
				&& kp.QInv.Equals(this.qInv);
		}

		public override int GetHashCode()
		{
			return this.DP.GetHashCode() ^ this.DQ.GetHashCode() ^ this.Exponent.GetHashCode() ^ this.Modulus.GetHashCode()
				^ this.P.GetHashCode() ^ this.Q.GetHashCode() ^ this.PublicExponent.GetHashCode() ^ this.QInv.GetHashCode();
		}

		private static void ValidateValue(BigInteger x, string name, string desc)
		{
			if(x == null)
				throw new ArgumentNullException(name);
			if(x.SignValue <= 0)
				throw new ArgumentException("Not a valid RSA " + desc, name);
		}
	}
}