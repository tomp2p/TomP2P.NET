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

        /// <summary>
        /// Creates a bag for the peers. This peer knows a lot about close peers. The further away the peers are,
        /// the less known they are. Distance is measured with XOR of the peer ID.
        /// E.g., the distance of peer with ID 0x12 and peer with ID 0x28 is 0x3a. 
        /// </summary>
        /// <param name="peerMapConfiguration">The configuration values for this map.</param>
        public PeerMap(PeerMapConfiguration peerMapConfiguration)
        {
            _self = peerMapConfiguration.Self;
            if (_self == null || _self.IsZero)
            {
                throw new ArgumentException("Zero or null are not valid peer IDs.");
            }
            _bagSizeVerified = peerMapConfiguration.BagSizeVerified;
            _bagSizeOverflow = peerMapConfiguration.BagSizeOverflow;
            _offlineCount = peerMapConfiguration.OfflineCount;
            _peerFilters = peerMapConfiguration.PeerFilters;
            _peerMapVerified = InitFixedMap(_bagSizeVerified, false);
            _peerMapOverflow = InitFixedMap(_bagSizeVerified, true);
            // _bagSizeVerified * Number160.Bits should be enough
            _offlineMap = new ConcurrentCacheMap<Number160, PeerAddress>(peerMapConfiguration.OfflineTimeout, _bagSizeVerified * Number160.Bits);
            _shutdownMap = new ConcurrentCacheMap<Number160, PeerAddress>(peerMapConfiguration.ShutdownTimeout, _bagSizeVerified * Number160.Bits);
            _exceptionMap = new ConcurrentCacheMap<Number160, PeerAddress>(peerMapConfiguration.ExceptionTimeout, _bagSizeVerified * Number160.Bits);
            _maintenance = peerMapConfiguration.Maintenance.Init(_peerMapVerified, _peerMapOverflow, _offlineMap,
                _shutdownMap, _exceptionMap);
            _peerVerification = peerMapConfiguration.IsPeerVerification;
        }

        /// <summary>
        /// Creates a fixed size bag with an unmodifiable map.
        /// </summary>
        /// <param name="bagSize">The bag size.</param>
        /// <param name="caching">If a caching map should be created.</param>
        /// <returns>The list of bags containing an unmodifiable map.</returns>
        private static IList<IDictionary<Number160, PeerStatistic>> InitFixedMap(int bagSize, bool caching)
        {
            var tmp = new List<IDictionary<Number160, PeerStatistic>>();
            for (int i = 0; i < Number160.Bits; i++)
            {
                if (caching)
                {
                    tmp.Add(new CacheMap<Number160, PeerStatistic>(bagSize, true));
                }
                else
                {
                    int memAlloc = Math.Max(0, bagSize - (Number160.Bits - i));
                    tmp.Add(new Dictionary<Number160, PeerStatistic>(memAlloc));
                }
            }
            return tmp.AsReadOnly();
        }



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
