using Redstone.ServiceNode.Models;
using Stratis.Bitcoin.EventBus;

namespace Redstone.ServiceNode.Events
{
    /// <summary>
    /// Event that is executed when service node is removed.
    /// </summary>
    /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
    public class ServiceNodeRemoved : EventBase
    {
        public IServiceNode RemovedNode { get; }

        public ServiceNodeRemoved(IServiceNode removedNode)
        {
            this.RemovedNode = removedNode;
        }
    }
}
