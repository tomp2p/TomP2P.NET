using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Connection;

namespace TomP2P.Peers
{
    /// <summary>
    /// This routing implementation is based on Kademlia.
    /// However, many changes have been applied to make it faster and more flexible.
    /// This class is partially thread-safe.
    /// </summary>
    public class PeerMap : IPeerStatusListener, IMaintainable
    {
        // TODO implement PeerMap

        public bool PeerFailed(PeerAddress remotePeer, PeerException exception)
        {
            throw new NotImplementedException();
        }

        public bool PeerFound(PeerAddress remotePeer, PeerAddress referrer, PeerConnection peerConnection)
        {
            throw new NotImplementedException();
        }
    }
}
