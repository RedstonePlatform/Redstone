using System;

namespace NBitcoin
{
	public class PuzzlePaymentRequest
	{
		public PuzzlePaymentRequest(Puzzle puzzle, Money amount, LockTime escrowDate)
		{
			if(puzzle == null)
				throw new ArgumentNullException(nameof(puzzle));
            this.Amount = amount ?? throw new ArgumentNullException(nameof(amount));
			this.EscrowDate = escrowDate;
			this.RsaPubKeyHash = puzzle.RsaPubKey.GetHash();
			this.PuzzleValue = puzzle.PuzzleValue;
		}
		public uint256 RsaPubKeyHash
		{
			get; set;
		}
		public PuzzleValue PuzzleValue
		{
			get; set;
		}
		public Money Amount
		{
			get; set;
		}
		public LockTime EscrowDate
		{
			get; set;
		}
	}
}

