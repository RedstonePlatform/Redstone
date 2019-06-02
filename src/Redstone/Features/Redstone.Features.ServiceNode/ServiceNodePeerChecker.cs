namespace Redstone.Features.ServiceNode
{
    public class ServiceNodePeerChecker
    {
        //private async Task PerformPeerCheckAsync()
        //{
        //    // TODO: remove own record
        //    var registrations = this.registrationStore.GetAll().ToList();

        //    var peerToCheck = registrations.OrderBy(a => RandomUtils.GetInt32()).FirstOrDefault();

        //    if (peerToCheck != null)
        //    {
        //        var nodeIpAddress = peerToCheck.Token.Ipv4Addr != null && peerToCheck.Token.Ipv4Addr != IPAddress.None
        //            ? peerToCheck.Token.Ipv4Addr
        //            : peerToCheck.Token.Ipv6Addr != null && peerToCheck.Token.Ipv6Addr != IPAddress.IPv6None
        //            ? peerToCheck.Token.Ipv6Addr
        //            : null;
        //        var nodePort = peerToCheck.Token.Port;

        //        if (nodeIpAddress != null)
        //        {
        //            // TODO: ping server ip and api ip, also get api base
        //            var peerEndpoint = new IPEndPoint(nodeIpAddress, nodePort);
        //            //var peer = await this.connectionManager.ConnectAsync(peerEndpoint).ConfigureAwait(false);
        //            //peer.IsConnected
        //        }
        //    }
        //}
    }
}