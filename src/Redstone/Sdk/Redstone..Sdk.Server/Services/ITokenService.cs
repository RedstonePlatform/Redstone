using System.Threading.Tasks;

namespace Redstone.Sdk.Server.Services
{
    public interface ITokenService
    {
        string GetHex();
        bool ValidateHex(long minPayment);
        Task<string> AcceptHex(long minPayment, string key);
        string GetToken();
        bool ValidateToken(string key);
    }
}
