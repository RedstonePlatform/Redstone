using System.Threading.Tasks;
using Redstone.Sdk.Models;

namespace Redstone.Sdk.Services
{
    public interface IWalletService
    {
        Task<WalletBuildTransactionModel> BuildTransactionAsync(BuildTransactionRequest request);
        Task<WalletSendTransactionModel> SendTransactionAsync(SendTransactionRequest request);
    }
}