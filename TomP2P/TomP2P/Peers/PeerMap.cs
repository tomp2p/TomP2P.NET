using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.P2P;
using TomP2P.Utils;

namespace TomP2P.Peers
{
    /// <summary>
    /// This routing implementation is based on Kademlia.
    /// However, many changes have been applied to make it faster and more flexible.
    /// This class is partially thread-safe.
    /// </summary>
    public class PeerMap : IPeerStatusListener, IMaintainable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // each distance bit has its own bag
        // this is the size of the verified peers (the ones we know are reachable)
        private readonly int _bagSizeVerified;
        private readonly int _bagSizeOverflow;

        // the ID of this node
        private readonly Number160 _self;

        // the storage for the peers that are verified
        private readonly IList<IDictionary<Number160, PeerStatistic>> _peerMapVerified;
 
        // the storage for the peers that are not verified or overflown
        private readonly IList<IDictionary<Number160, PeerStatistic>> _peerMapOverflow;

        private readonly ConcurrentCacheMap<Number160, PeerAddress> _offlineMap;
        private readonly ConcurrentCacheMap<Number160, PeerAddress> _shutdownMap;
        private readonly ConcurrentCacheMap<Number160, PeerAddress> _exceptionMap;

        // stores listeners that will be notified if a peer gets removed or added
        private readonly IList<IPeerMapChangeListener> _peerMapChangeListeners = new List<IPeerMapChangeListener>();

        private readonly ICollection<IPeerFilter> _peerFilters;

        // the number of failures until a peer is considered offline
        private readonly int _offlineCount;
        private readonly IMaintenance _maintenance;
        private readonly bool _peerVerification;

        public PeerMap(PeerMapConfiguration)

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
