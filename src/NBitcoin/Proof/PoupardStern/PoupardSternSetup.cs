using System;

namespace NBitcoin.Proof.PoupardStern
{
    public class PoupardSternSetup
    {
        public PoupardSternSetup()
        {

        }
        public PoupardSternSetup(byte[] publicString, int keySize, int securityParameter = 128)
        {
            if (this.KeySize < 0)
                throw new ArgumentOutOfRangeException(nameof(keySize));
            this.SecurityParameter = securityParameter;
            this.PublicString = publicString ?? throw new ArgumentNullException(nameof(publicString));
            this.KeySize = keySize;
        }
        public byte[] PublicString
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

        public PoupardSternSetup Clone()
        {
            return new PoupardSternSetup()
            {
                KeySize = this.KeySize,
                SecurityParameter = this.SecurityParameter,
                PublicString = this.PublicString
            };
        }
    }
}
