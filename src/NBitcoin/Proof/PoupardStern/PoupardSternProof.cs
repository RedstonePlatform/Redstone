using System;
using NBitcoin.BouncyCastle.Math;

namespace NBitcoin.Proof.PoupardStern
{
    public class PoupardSternProof
    {
        internal PoupardSternProof(Tuple<BigInteger[], BigInteger> proof)
        {
            if(proof == null)
                throw new ArgumentNullException(nameof(proof));
            this.XValues = proof.Item1;
            this.YValue = proof.Item2;
        }
        internal BigInteger[] XValues
        {
            get; set;
        }
        internal BigInteger YValue
        {
            get; set;
        }
    }
}