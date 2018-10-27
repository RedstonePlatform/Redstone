using System;

namespace NBitcoin.Proof.PermutationTest
{
    public class PermutationTestProof
    {
        public PermutationTestProof(byte[][] proof)
        {
            this.Signatures = proof ?? throw new ArgumentNullException(nameof(proof));
        }

        public byte[][] Signatures
        {
            get; set;
        }
    }
}
