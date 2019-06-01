﻿using System.ComponentModel.DataAnnotations;
using Stratis.Bitcoin.Features.Wallet.Models;

namespace Redstone.ServiceNode.Models
{
    public class RegisterServiceNodeRequest : RequestModel
    {
        [Required(ErrorMessage = "The name of the wallet is required.")]
        public string WalletName { get; set; }

        [Required(ErrorMessage = "The name of the wallet account is required.")]
        public string AccountName { get; set; }

        [Required(ErrorMessage = "The wallet password is required.")]
        public string Password { get; set; }
    }
}
