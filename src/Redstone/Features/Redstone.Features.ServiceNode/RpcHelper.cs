using System;
using System.Net;
using NBitcoin;
using Stratis.Bitcoin.Features.RPC;

namespace Redstone.Features.ServiceNode
{
    public class RpcHelper
    {
        Network rpcNetwork;

        public RpcHelper(Network network)
        {
            this.rpcNetwork = network;
        }

        public RPCClient GetClient(string rpcUser, string rpcPassword, string rpcUrl)
        {
            RPCClient rpc = new RPCClient(rpcUser + ":" + rpcPassword, new Uri(rpcUrl), this.rpcNetwork);
            return rpc;
        }
    }
}
