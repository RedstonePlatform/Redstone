﻿using System;
using NBitcoin.BouncyCastle.crypto.parameters;
using NBitcoin.BouncyCastle.Crypto;

namespace NBitcoin.BouncyCastle.crypto.generators
{
    /**
    * Generator for MGF1 as defined in Pkcs 1v2
    */

    internal class Mgf1BytesGenerator
    {
        private IDigest digest;
        private byte[] seed;
        private int hLen;

        /**
        * @param digest the digest to be used as the source of Generated bytes
        */
        public Mgf1BytesGenerator(
            IDigest digest)
        {
            this.digest = digest;
            this.hLen = digest.GetDigestSize();
        }

        public void Init(MgfParameters parameters)
        {
            MgfParameters p = parameters;
            this.seed = p.GetSeed();
        }

        /**
        * return the underlying digest.
        */
        public IDigest Digest
        {
            get
            {
                return this.digest;
            }
        }

        /**
        * int to octet string.
        */
        private void ItoOSP(
            int i,
            byte[] sp)
        {
            sp[0] = (byte)((uint)i >> 24);
            sp[1] = (byte)((uint)i >> 16);
            sp[2] = (byte)((uint)i >> 8);
            sp[3] = (byte)((uint)i >> 0);
        }

        /**
        * fill len bytes of the output buffer with bytes Generated from
        * the derivation function.
        *
        * @throws DataLengthException if the out buffer is too small.
        */
        public int GenerateBytes(
            byte[] output,
            int outOff,
            int length)
        {
            if((output.Length - length) < outOff)
            {
                throw new DataLengthException("output buffer too small");
            }

            byte[] hashBuf = new byte[this.hLen];
            byte[] C = new byte[4];
            int counter = 0;

            this.digest.Reset();

            if(length > this.hLen)
            {
                do
                {
                    ItoOSP(counter, C);

                    this.digest.BlockUpdate(this.seed, 0, this.seed.Length);
                    this.digest.BlockUpdate(C, 0, C.Length);
                    this.digest.DoFinal(hashBuf, 0);

                    Array.Copy(hashBuf, 0, output, outOff + counter * this.hLen, this.hLen);
                }
                while(++counter < (length / this.hLen));
            }

            if((counter * this.hLen) < length)
            {
                ItoOSP(counter, C);

                this.digest.BlockUpdate(this.seed, 0, this.seed.Length);
                this.digest.BlockUpdate(C, 0, C.Length);
                this.digest.DoFinal(hashBuf, 0);

                Array.Copy(hashBuf, 0, output, outOff + counter * this.hLen, length - (counter * this.hLen));
            }

            return length;
        }
    }

}