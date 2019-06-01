using System;

namespace Redstone.Features.ServiceNode
{
    public interface IRegistrationManager : IDisposable
    {
        void Initialize();
    }
}
