using System;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
    public class PuzzleSolution : IBitcoinSerializable
    {
        public PuzzleSolution()
        {

        }
        public PuzzleSolution(byte[] solution)
        {
            if(solution == null)
                throw new ArgumentNullException(nameof(solution));
            this._Value = new BigInteger(1, solution);
        }

        internal PuzzleSolution(BigInteger value)
        {
            this._Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        internal BigInteger _Value;

        public byte[] ToBytes()
        {
            return this._Value.ToByteArrayUnsigned();
        }

        public override bool Equals(object obj)
        {
            PuzzleSolution item = obj as PuzzleSolution;
            if(item == null)
                return false;
            return this._Value.Equals(item._Value);
        }
        public static bool operator ==(PuzzleSolution a, PuzzleSolution b)
        {
            if(ReferenceEquals(a, b))
                return true;
            if(((object)a == null) || ((object)b == null))
                return false;
            return a._Value.Equals(b._Value);
        }

        public static bool operator !=(PuzzleSolution a, PuzzleSolution b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this._Value.GetHashCode();
        }

        public PuzzleSolution Unblind(RsaPubKey rsaPubKey, BlindFactor blind)
        {
            if(rsaPubKey == null)
                throw new ArgumentNullException(nameof(rsaPubKey));
            if(blind == null)
                throw new ArgumentNullException(nameof(blind));
            return new PuzzleSolution(rsaPubKey.Unblind(this._Value, blind));
        }

        public override string ToString()
        {
            return Encoders.Hex.EncodeData(ToBytes());
        }

        public void ReadWrite(BitcoinStream stream)
        {
            stream.ReadWriteC(ref this._Value);
        }
    }
}
