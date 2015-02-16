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
            // TODO implement
            return true;
            //throw new NotImplementedException();
        }

        public bool PeerFound(PeerAddress remotePeer, PeerAddress referrer, PeerConnection peerConnection)
        {
            // TODO implement
            return true;
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an XOR comparer based on this peer ID.
        /// </summary>
        /// <returns>The XOR comparer.</returns>
        public IComparer<PeerAddress> CreateComparer()
        {
            // TODO implement
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates the Kademlia distance comparer.
        /// </summary>
        /// <param name="id">The ID of this peer.</param>
        /// <returns>The XOR comparer.</returns>
        public static IComparer<PeerAddress> CreateComparer(Number160 id)
        {
            throw new NotImplementedException();
        }

        public ICollection<PeerAddress> ClosePeers(Number160 number160, int p)
        {
            throw new NotImplementedException();
        }

        public IList<PeerAddress> All
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IList<PeerAddress> AllOverflow
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
