using System.Threading.Tasks;

namespace Redstone.Features.ServiceNode
{
    public interface IServiceNodeCollateralChecker
    {
        Task InitializeAsync();
    }
}