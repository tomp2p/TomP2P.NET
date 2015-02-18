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

        /// <summary>
        /// The ID of this node.
        /// Each node that has a bag has an ID itself to define what is close.
        /// </summary>
        public Number160 Self { get; private set; }

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
            Self = peerMapConfiguration.Self;
            if (Self == null || Self.IsZero)
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

        /// <summary>
        /// Adds a map change listener. This is thread-safe.
        /// </summary>
        /// <param name="peerMapChangeListener">The listener to be added.</param>
        public void AddPeerMapChangeListener(IPeerMapChangeListener peerMapChangeListener)
        {
            lock (_peerMapChangeListeners)
            {
                _peerMapChangeListeners.Add(peerMapChangeListener);
            }
        }

        /// <summary>
        /// Removes a map change listener. This is thread-safe.
        /// </summary>
        /// <param name="peerMapChangeListener">The listener to be removed.</param>
        public void RemovePeerMapChangeListener(IPeerMapChangeListener peerMapChangeListener)
        {
            lock (_peerMapChangeListeners)
            {
                _peerMapChangeListeners.Remove(peerMapChangeListener);
            }
        }

        /// <summary>
        /// Notifies on insert. This is called after the peer has been added to the map.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="peerAddress">The address of the inserted peer.</param>
        /// <param name="verified">True, if the peer was inserted into the verified map.</param>
        private void NotifyInsert(PeerAddress peerAddress, bool verified)
        {
            lock (_peerMapChangeListeners)
            {
                foreach (var listener in _peerMapChangeListeners)
                {
                    listener.PeerInserted(peerAddress, verified);
                }
            }
        }

        /// <summary>
        /// Notifies on remove. This method is thread-safe.
        /// </summary>
        /// <param name="peerAddress">The address of the removed peer.</param>
        /// <param name="storedPeerAddress">Contains statistical information.</param>
        private void NotifyRemove(PeerAddress peerAddress, PeerStatistic storedPeerAddress)
        {
            lock (_peerMapChangeListeners)
            {
                foreach (var listener in _peerMapChangeListeners)
                {
                    listener.PeerRemoved(peerAddress, storedPeerAddress);
                }
            }
        }

        /// <summary>
        /// Notifies on update. This method is thread-safe.
        /// </summary>
        /// <param name="peerAddress">The address of the updated peer.</param>
        /// <param name="storedPeerAddress">Contains statistical information.</param>
        private void NotifyUpdate(PeerAddress peerAddress, PeerStatistic storedPeerAddress)
        {
            lock (_peerMapChangeListeners)
            {
                foreach (var listener in _peerMapChangeListeners)
                {
                    listener.PeerUpdated(peerAddress, storedPeerAddress);
                }
            }
        }

        /// <summary>
        /// The number of the peers in the verified map.
        /// </summary>
        public int Size
        {
            get
            {
                int size = 0;
                foreach (var map in _peerMapVerified)
                {
                    lock (map)
                    {
                        size += map.Count;
                    }
                }
                return size;
            }
        }

        private bool Reject(PeerAddress peerAddress)
        {
            if (_peerFilters == null || _peerFilters.Count == 0)
            {
                return false;
            }
            ICollection<PeerAddress> all = All;
            foreach (var filter in _peerFilters)
            {
                if (filter.Reject(peerAddress, all, Self))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds a neighbor to the neighbor list. If the bag is full, the ID zero or the same
        /// as our ID, the neighbor is not added. This method is thread-safe.
        /// </summary>
        /// <param name="remotePeer">The node to be added.</param>
        /// <param name="referrer">If we had direct contact and we know for sure that this node 
        /// is online, we set first hand to true. Information from 3rd party peers are always 
        /// second hand and treated as such </param>
        /// <param name="peerConnection"></param>
        /// <returns>True, if the neighbor could be added or updated. False, otherwise.</returns>
        public bool PeerFound(PeerAddress remotePeer, PeerAddress referrer, PeerConnection peerConnection)
        {
            // TODO implement
            return true;
            //throw new NotImplementedException();
        }

        public bool PeerFailed(PeerAddress remotePeer, PeerException exception)
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
