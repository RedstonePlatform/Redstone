using System;
using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.BouncyCastle.Math;

namespace NBitcoin.BouncyCastle.asn1.pkcs
{
    internal class RsaPublicKeyStructure
        : Asn1Encodable
    {
        private BigInteger modulus;
        private BigInteger publicExponent;


        public static RsaPublicKeyStructure GetInstance(
            object obj)
        {
            if(obj == null || obj is RsaPublicKeyStructure)
            {
                return (RsaPublicKeyStructure)obj;
            }

            if(obj is Asn1Sequence)
            {
                return new RsaPublicKeyStructure((Asn1Sequence)obj);
            }

            throw new ArgumentException("Invalid RsaPublicKeyStructure: " + obj.GetType().Name);
        }

        public RsaPublicKeyStructure(
            BigInteger modulus,
            BigInteger publicExponent)
        {
            if(modulus == null)
                throw new ArgumentNullException(nameof(modulus));
            if(publicExponent == null)
                throw new ArgumentNullException(nameof(publicExponent));
            if(modulus.SignValue <= 0)
                throw new ArgumentException("Not a valid RSA modulus", nameof(modulus));
            if(publicExponent.SignValue <= 0)
                throw new ArgumentException("Not a valid RSA public exponent", nameof(publicExponent));

            this.modulus = modulus;
            this.publicExponent = publicExponent;
        }

        internal RsaPublicKeyStructure(
            Asn1Sequence seq)
        {
            if(seq.Count != 2)
                throw new ArgumentException("Bad sequence size: " + seq.Count);

            // Note: we are accepting technically incorrect (i.e. negative) values here
            this.modulus = DerInteger.GetInstance(seq[0]).PositiveValue;
            this.publicExponent = DerInteger.GetInstance(seq[1]).PositiveValue;
        }

        public BigInteger Modulus
        {
            get
            {
                return this.modulus;
            }
        }

        public BigInteger PublicExponent
        {
            get
            {
                return this.publicExponent;
            }
        }

        /**
         * This outputs the key in Pkcs1v2 format.
         * <pre>
         *      RSAPublicKey ::= Sequence {
         *                          modulus Integer, -- n
         *                          publicExponent Integer, -- e
         *                      }
         * </pre>
         */
        public override Asn1Object ToAsn1Object()
        {
            return new DerSequence(
                new DerInteger(this.Modulus),
                new DerInteger(this.PublicExponent));
        }
    }
}