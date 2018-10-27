using System;

namespace NBitcoin.Proof.PermutationTest
{
    public class PermutationTestSetup
    {
        public PermutationTestSetup()
        {

        }

        public PermutationTestSetup(byte[] publicString, int alpha, int keySize, int securityParameter = 128)
        {
            if (this.KeySize < 0)
                throw new ArgumentOutOfRangeException(nameof(keySize));
            this.Alpha = alpha;
            this.PublicString = publicString ?? throw new ArgumentNullException(nameof(publicString));
            this.KeySize = keySize;
            this.SecurityParameter = securityParameter;
        }
        public byte[] PublicString
        {
            get; set;
        }
        public int Alpha
        {
            get; set;
        }
        public int SecurityParameter
        {
            get; set;
        } = 128;

        public int KeySize
        {
            get; set;
        }

        public PermutationTestSetup Clone()
        {
            return new PermutationTestSetup()
            {
                KeySize = this.KeySize,
                Alpha = this.Alpha,
                SecurityParameter = this.SecurityParameter,
                PublicString = this.PublicString
            };
        }
    }
}
