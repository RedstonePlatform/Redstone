﻿using System.Collections.Generic;

namespace Stratis.Bitcoin.Features.BlockStore.AddressIndexing
{
    public class AddressIndexerData
    {
        /// <summary>Id required for litedb.</summary>
        public int Id { get; set; }

        public byte[] TipHashBytes { get; set; }

        /// <summary>Address changes by address.</summary>
        public Dictionary<string, List<AddressBalanceChange>> AddressChanges { get; set; }
    }

    public class AddressBalanceChange
    {
        /// <summary><c>true</c> if there was a deposit to an address, <c>false</c> if it was a withdrawal.</summary>
        public bool Deposited { get; set; }

        public long Satoshi { get; set; }

        /// <summary>Height of a block in which operation was confirmed.</summary>
        public int BalanceChangedHeight { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(this.Deposited)}:{this.Deposited}, {nameof(this.Satoshi)}:{this.Satoshi}, {nameof(this.BalanceChangedHeight)}:{this.BalanceChangedHeight}";
        }
    }
}
