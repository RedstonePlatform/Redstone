using System;
using NBitcoin.BouncyCastle.asn1.pkcs;
using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.BouncyCastle.crypto.engines;
using NBitcoin.BouncyCastle.crypto.generators;
using NBitcoin.BouncyCastle.crypto.parameters;
using NBitcoin.BouncyCastle.Crypto.Digests;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.Crypto;

namespace NBitcoin
{
    public class RsaPubKey
    {
        public RsaPubKey()
        {

        }

        public RsaPubKey(byte[] bytes)
        {
            if(bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            try
            {
                DerSequence seq2 = RsaKey.GetRSASequence(bytes);
                var s = new RsaPublicKeyStructure(seq2);
                this._Key = new RsaKeyParameters(false, s.Modulus, s.PublicExponent);
            }
            catch(Exception)
            {
                throw new FormatException("Invalid RSA Key");
            }
        }

        internal readonly RsaKeyParameters _Key;
        internal RsaPubKey(RsaKeyParameters key)
        {
            this._Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public byte[] ToBytes()
        {
            RsaPublicKeyStructure keyStruct = new RsaPublicKeyStructure(
                this._Key.Modulus,
                this._Key.Exponent);
            var privInfo = new PrivateKeyInfo(RsaKey.algID, keyStruct.ToAsn1Object());
            return privInfo.ToAsn1Object().GetEncoded();
        }

        public bool Verify(byte[] signature, byte[] data, uint160 nonce)
        {
            byte[] output = new byte[256];
            var msg = Utils.Combine(nonce.ToBytes(), data);
            Sha512Digest sha512 = new Sha512Digest();
            var generator = new Mgf1BytesGenerator(sha512);
            generator.Init(new MgfParameters(msg));
            generator.GenerateBytes(output, 0, output.Length);
            var input = new BigInteger(1, output);
            if(input.CompareTo(this._Key.Modulus) >= 0)
                return false;
            if(signature.Length > 256)
                return false;
            var signatureInt = new BigInteger(1, signature);
            if(signatureInt.CompareTo(this._Key.Modulus) >= 0)
                return false;
            var engine = new RsaBlindedEngine();
            engine.Init(false, this._Key);
            return input.Equals(engine.ProcessBlock(signatureInt));
        }

        public uint256 GetHash()
        {
            return Hashes.Hash256(ToBytes());
        }

        public Puzzle GeneratePuzzle(ref PuzzleSolution solution)
        {
            solution = solution ?? new PuzzleSolution(Utils.GenerateEncryptableInteger(this._Key));
            return new Puzzle(this, new PuzzleValue(Encrypt(solution._Value)));
        }

        internal BigInteger Encrypt(BigInteger data)
        {
            if(data == null)
                throw new ArgumentNullException(nameof(data));
            if(data.CompareTo(this._Key.Modulus) >= 0)
                throw new ArgumentException("input too large for RSA cipher.");
                
            RsaBlindedEngine engine = new RsaBlindedEngine();
            engine.Init(true, this._Key);
            return engine.ProcessBlock(data);
        }

        internal BigInteger Blind(BigInteger data, ref BlindFactor blindFactor)
        {
            if(data == null)
                throw new ArgumentNullException(nameof(data));
            EnsureInitializeBlindFactor(ref blindFactor);
            return Blind(blindFactor._Value.ModPow(this._Key.Exponent, this._Key.Modulus), data);
        }

        private void EnsureInitializeBlindFactor(ref BlindFactor blindFactor)
        {
            blindFactor = blindFactor ?? new BlindFactor(Utils.GenerateEncryptableInteger(this._Key));
        }

        internal BigInteger RevertBlind(BigInteger data, BlindFactor blindFactor)
        {
            if(data == null)
                throw new ArgumentNullException(nameof(data));
            if(blindFactor == null)
                throw new ArgumentNullException(nameof(blindFactor));
            EnsureInitializeBlindFactor(ref blindFactor);
            var ai = blindFactor._Value.ModInverse(this._Key.Modulus);
            return Blind(ai.ModPow(this._Key.Exponent, this._Key.Modulus), data);
        }

        internal BigInteger Unblind(BigInteger data, BlindFactor blindFactor)
        {
            if(data == null)
                throw new ArgumentNullException(nameof(data));
            if(blindFactor == null)
                throw new ArgumentNullException(nameof(blindFactor));
            EnsureInitializeBlindFactor(ref blindFactor);
            return Blind(blindFactor._Value.ModInverse(this._Key.Modulus), data);
        }

        internal BigInteger Blind(BigInteger multiplier, BigInteger msg)
        {
            return msg.Multiply(multiplier).Mod(this._Key.Modulus);
        }

        public int GetKeySize()
        {
            return this._Key.Modulus.BitLength;
        }

        public override bool Equals(object obj)
        {
            RsaPubKey item = obj as RsaPubKey;
            if(item == null)
                return false;
            return this._Key.Equals(item._Key);
        }
        public static bool operator ==(RsaPubKey a, RsaPubKey b)
        {
            if(ReferenceEquals(a, b))
                return true;
            if(((object)a == null) || ((object)b == null))
                return false;
            return a._Key.Equals(b._Key);
        }

        public static bool operator !=(RsaPubKey a, RsaPubKey b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this._Key.GetHashCode();
        }
    }
}
