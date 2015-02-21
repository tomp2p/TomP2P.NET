using System.Collections.Generic;

namespace TomP2P.Peers
{
    /// <summary>
    /// The default peer filter accepts all peers.
    /// </summary>
    public class DefaultPeerFilter : IPeerFilter
    {
        public bool Reject(PeerAddress peerAddress, ICollection<PeerAddress> all, Number160 target)
        {
            // by default, don't reject anything
            return false;
        }
    }
}
