using System.ComponentModel.DataAnnotations;
using Stratis.Bitcoin.Features.Wallet.Models;

namespace Redstone.ServiceNode.Models
{
    public class RegisterServiceNodeRequest : RequestModel
    {
        [Required(ErrorMessage = "The name of the wallet (required).")]
        public string WalletName { get; set; }

        [Required(ErrorMessage = "The name of the wallet account (required).")]
        public string AccountName { get; set; }

        [Required(ErrorMessage = "The wallet password (required).")]
        public string Password { get; set; }
    }
}
