using Redstone.ServiceNode.Models;
using Stratis.Bitcoin.EventBus;

namespace Redstone.ServiceNode.Events
{
    /// <summary>
    /// Event that is executed when a new service node is added.
    /// </summary>
    /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
    public class ServiceNodeAdded : EventBase
    {
        public IServiceNode AddedNode { get; }

        public ServiceNodeAdded(IServiceNode addedNode)
        {
            this.AddedNode = addedNode;
        }
    }
}
