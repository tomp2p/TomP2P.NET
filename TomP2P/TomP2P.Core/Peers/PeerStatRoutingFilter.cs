using System.Collections.Generic;
using TomP2P.Core.P2P;

namespace TomP2P.Core.Peers
{
    /// <summary>
    /// The default filter accepts all peers.
    /// </summary>
    public class PeerStatRoutingFilter : IPeerFilter
    {
        //private readonly Statistics _statistics;
        //private readonly int _replicationRate;

        public PeerStatRoutingFilter(Statistics statistics, int replicationRate)
        {
            //_statistics = statistics;
            //_replicationRate = replicationRate;
        }

        public bool Reject(PeerAddress peerAddress, ICollection<PeerAddress> all, Number160 target)
        {
            // TODO in Java: to be implemented
            return false;
        }
    }
}
