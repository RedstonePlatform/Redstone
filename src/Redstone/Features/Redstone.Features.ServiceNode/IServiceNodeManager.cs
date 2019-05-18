using System;

namespace Redstone.Features.ServiceNode
{
    public interface IServiceNodeManager : IDisposable
    {
        void Initialize();
    }
}
