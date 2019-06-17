namespace Redstone.ServiceNode.Models
{
    public class RegisterServiceNodeResponse
    {
        public string RegistrationTxHash { get; set; }

        public string CollateralPubKeyHash { get; set; }

        public string RewardPubKeyHash { get; set; }
    }
}
