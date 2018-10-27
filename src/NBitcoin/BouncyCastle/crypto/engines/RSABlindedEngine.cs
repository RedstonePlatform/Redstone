using System;
using NBitcoin.BouncyCastle.crypto.parameters;
using NBitcoin.BouncyCastle.Crypto;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Security;
using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.crypto.engines
{

    /**
    NOTE: MODIFIED FROM ORIGINAL VERSION 

    - `ProcessBlock` was modified to accept BigInteger as input and output BigInteger
    - Added `ConvertOutput` that convert BigInteger to byte[]
    */

    /**
     * this does your basic RSA algorithm with blinding
     */
    internal class RsaBlindedEngine
        // : IAsymmetricBlockCipher
    {
        private readonly RsaCoreEngine core = new RsaCoreEngine();
        private RsaKeyParameters key;
        private SecureRandom random;

        public virtual string AlgorithmName
        {
            get { return "RSA"; }
        }

        /**
         * initialise the RSA engine.
         *
         * @param forEncryption true if we are encrypting, false otherwise.
         * @param param the necessary RSA key parameters.
         */
        public virtual void Init(
            bool forEncryption,
            ICipherParameters param)
        {
            this.core.Init(forEncryption, param);

            if (param is ParametersWithRandom)
            {
                ParametersWithRandom rParam = (ParametersWithRandom)param;

                this.key = (RsaKeyParameters)rParam.Parameters;
                this.random = rParam.Random;
            }
            else
            {
                this.key = (RsaKeyParameters)param;
                this.random = new SecureRandom();
            }
        }

        public virtual byte[] ConvertOutput(
            BigInteger result)
        {
            return this.core.ConvertOutput(result);
        }


        /**
         * Return the maximum size for an input block to this engine.
         * For RSA this is always one byte less than the key size on
         * encryption, and the same length as the key size on decryption.
         *
         * @return maximum size for an input block.
         */
        public virtual int GetInputBlockSize()
        {
            return this.core.GetInputBlockSize();
        }

        /**
         * Return the maximum size for an output block to this engine.
         * For RSA this is always one byte less than the key size on
         * decryption, and the same length as the key size on encryption.
         *
         * @return maximum size for an output block.
         */
        public virtual int GetOutputBlockSize()
        {
            return this.core.GetOutputBlockSize();
        }

        public virtual BigInteger ProcessBlock(BigInteger input)
        {
            if (this.key == null)
                throw new InvalidOperationException("RSA engine not initialised");

            // BigInteger input = core.ConvertInput(inBuf, inOff, inLen);

            BigInteger result;
            if (this.key is RsaPrivateCrtKeyParameters)
            {
                RsaPrivateCrtKeyParameters k = (RsaPrivateCrtKeyParameters)this.key;
                BigInteger e = k.PublicExponent;
                if (e != null)   // can't do blinding without a public exponent
                {
                    BigInteger m = k.Modulus;
                    BigInteger r = BigIntegers.CreateRandomInRange(
                        BigInteger.One, m.Subtract(BigInteger.One), this.random);

                    BigInteger blindedInput = r.ModPow(e, m).Multiply(input).Mod(m);
                    BigInteger blindedResult = this.core.ProcessBlock(blindedInput);

                    BigInteger rInv = r.ModInverse(m);
                    result = blindedResult.Multiply(rInv).Mod(m);

                    // defence against Arjen Lenstra�s CRT attack
                    if (!input.Equals(result.ModPow(e, m)))
                        throw new InvalidOperationException("RSA engine faulty decryption/signing detected");
                }
                else
                {
                    result = this.core.ProcessBlock(input);
                }
            }
            else
            {
                result = this.core.ProcessBlock(input);
            }

            return result;
        }
    }
}
