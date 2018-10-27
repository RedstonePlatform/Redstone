using System;
using NBitcoin.BouncyCastle.Math;

namespace NBitcoin
{
    public class BlindFactor : IBitcoinSerializable
    {
        public BlindFactor()
        {

        }
        public BlindFactor(byte[] v)
        {
            if(v == null)
                throw new ArgumentNullException(nameof(v));
            _Value = new BigInteger(1, v);
        }

        internal BlindFactor(BigInteger v)
        {
            _Value = v ?? throw new ArgumentNullException(nameof(v));
        }

        internal BigInteger _Value;

        public byte[] ToBytes()
        {
            return _Value.ToByteArrayUnsigned();
        }

        public void ReadWrite(BitcoinStream stream)
        {
            stream.ReadWriteC(ref _Value);
        }
    }
}
