using System;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
    public class PuzzleValue : IBitcoinSerializable
    {
        internal BigInteger _Value;
        public PuzzleValue()
        {

        }
        public PuzzleValue(byte[] z)
        {
            if(z == null)
                throw new ArgumentNullException(nameof(z));
            this._Value = new BigInteger(1, z);
        }
        internal PuzzleValue(BigInteger z)
        {
            this._Value = z ?? throw new ArgumentNullException(nameof(z));
        }

        public byte[] ToBytes()
        {
            return this._Value.ToByteArrayUnsigned();
        }

        public override bool Equals(object obj)
        {
            PuzzleValue item = obj as PuzzleValue;
            if(item == null)
                return false;
            return this._Value.Equals(item._Value);
        }
        public static bool operator ==(PuzzleValue a, PuzzleValue b)
        {
            if(ReferenceEquals(a, b))
                return true;
            if(((object)a == null) || ((object)b == null))
                return false;
            return a._Value.Equals(b._Value);
        }

        public PuzzleSolution Solve(RsaKey key)
        {
            if(key == null)
                throw new ArgumentNullException(nameof(key));
            return key.SolvePuzzle(this);
        }

        public static bool operator !=(PuzzleValue a, PuzzleValue b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this._Value.GetHashCode();
        }

        public override string ToString()
        {
            return Encoders.Hex.EncodeData(ToBytes());
        }

        public Puzzle WithRsaKey(RsaPubKey key)
        {
            return new Puzzle(key, this);
        }

        public void ReadWrite(BitcoinStream stream)
        {
            stream.ReadWriteC(ref this._Value);
        }
    }
}
