using System.Threading.Tasks;

namespace Redstone.Sdk.Server.Services
{
    public interface ITokenService
    {
        string GetHex();
        bool ValidateHex();
        Task<string> AcceptHex();
        string GetToken();
        Task<bool> ValidateTokenAsync();
    }
}
