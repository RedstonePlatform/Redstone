using System;

namespace NBitcoin
{
    public class Puzzle
    {
        public Puzzle(RsaPubKey rsaPubKey, PuzzleValue puzzleValue)
        {
            this._RsaPubKey = rsaPubKey ?? throw new ArgumentNullException(nameof(rsaPubKey));
            this._PuzzleValue = puzzleValue ?? throw new ArgumentNullException(nameof(puzzleValue));
        }

        public Puzzle Blind(ref BlindFactor blind)
        {
            return new Puzzle(this._RsaPubKey, new PuzzleValue(this._RsaPubKey.Blind(this.PuzzleValue._Value, ref blind)));
        }

        public Puzzle Unblind(BlindFactor blind)
        {
            if(blind == null)
                throw new ArgumentNullException(nameof(blind));
            return new Puzzle(this._RsaPubKey, new PuzzleValue(this.RsaPubKey.RevertBlind(this.PuzzleValue._Value, blind)));
        }

        public PuzzleSolution Solve(RsaKey key)
        {
            if(key == null)
                throw new ArgumentNullException(nameof(key));
            return this.PuzzleValue.Solve(key);
        }

        public bool Verify(PuzzleSolution solution)
        {
            if(solution == null)
                throw new ArgumentNullException(nameof(solution));
            return this._RsaPubKey.Encrypt(solution._Value).Equals(this.PuzzleValue._Value);
        }


        private readonly RsaPubKey _RsaPubKey;
        public RsaPubKey RsaPubKey
        {
            get
            {
                return this._RsaPubKey;
            }
        }


        private readonly PuzzleValue _PuzzleValue;
        public PuzzleValue PuzzleValue
        {
            get
            {
                return this._PuzzleValue;
            }
        }


        public override bool Equals(object obj)
        {
            Puzzle item = obj as Puzzle;
            if(item == null)
                return false;
            return this.PuzzleValue.Equals(item.PuzzleValue) && this.RsaPubKey.Equals(item.RsaPubKey);
        }
        public static bool operator ==(Puzzle a, Puzzle b)
        {
            if(ReferenceEquals(a, b))
                return true;
            if(((object)a == null) || ((object)b == null))
                return false;
            return a.PuzzleValue == b.PuzzleValue && a.RsaPubKey == b.RsaPubKey;
        }

        public static bool operator !=(Puzzle a, Puzzle b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this.PuzzleValue.GetHashCode() ^ this.RsaPubKey.GetHashCode();
        }

        public override string ToString()
        {
            return this.PuzzleValue.ToString();
        }
    }
}
