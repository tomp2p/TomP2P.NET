using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using TomP2P.Connection;
using TomP2P.Extensions;
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
        public int BagSizeVerified { get; private set; }
        public int BagSizeOverflow { get; private set; }

        /// <summary>
        /// The ID of this node.
        /// Each node that has a bag has an ID itself to define what is close.
        /// </summary>
        public Number160 Self { get; private set; }

        // the storage for the peers that are verified
        public IList<IDictionary<Number160, PeerStatistic>> PeerMapVerified { get; private set; }

        // the storage for the peers that are not verified or overflown
        public IList<IDictionary<Number160, PeerStatistic>> PeerMapOverflow { get; private set; }

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
            BagSizeVerified = peerMapConfiguration.BagSizeVerified;
            BagSizeOverflow = peerMapConfiguration.BagSizeOverflow;
            _offlineCount = peerMapConfiguration.OfflineCount;
            _peerFilters = peerMapConfiguration.PeerFilters;
            PeerMapVerified = InitFixedMap(BagSizeVerified, false);
            PeerMapOverflow = InitFixedMap(BagSizeOverflow, true);
            // _bagSizeVerified * Number160.Bits should be enough
            _offlineMap = new ConcurrentCacheMap<Number160, PeerAddress>(peerMapConfiguration.OfflineTimeout, BagSizeVerified * Number160.Bits);
            _shutdownMap = new ConcurrentCacheMap<Number160, PeerAddress>(peerMapConfiguration.ShutdownTimeout, BagSizeVerified * Number160.Bits);
            _exceptionMap = new ConcurrentCacheMap<Number160, PeerAddress>(peerMapConfiguration.ExceptionTimeout, BagSizeVerified * Number160.Bits);
            _maintenance = peerMapConfiguration.Maintenance.Init(PeerMapVerified, PeerMapOverflow, _offlineMap,
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
                foreach (var map in PeerMapVerified)
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
        /// second hand and treated as suc.</param>
        /// <param name="peerConnection"></param>
        /// <returns>True, if the neighbor could be added or updated. False, otherwise.</returns>
        public bool PeerFound(PeerAddress remotePeer, PeerAddress referrer, PeerConnection peerConnection)
        {
            Logger.Debug("Peer {0} is online. Reporter was {1}.", remotePeer, referrer);
            bool firstHand = referrer == null;
            // if we got contacted by this peer, but we did not initiate the connection
            bool secondHand = remotePeer.Equals(referrer);
            // if a peer reported about other peers
            bool thirdHand = !firstHand && !secondHand;
            // always trust first hand information
            if (firstHand)
            {
                _offlineMap.Remove(remotePeer.PeerId);
                _shutdownMap.Remove(remotePeer.PeerId);
            }
            if (secondHand && !_peerVerification)
            {
                _offlineMap.Remove(remotePeer.PeerId);
                _shutdownMap.Remove(remotePeer.PeerId);
            }

            // don't add nodes with zero node ID, don't add myself and don't add nodes marked as bad
            if (remotePeer.PeerId.IsZero || Self.Equals(remotePeer.PeerId) || Reject(remotePeer))
            {
                return false;
            }

            if (remotePeer.IsFirewalledTcp || remotePeer.IsFirewalledUdp)
            {
                return false;
            }

            // if a peer is relayed but cannot provide any relays, it is useless
            if (remotePeer.IsRelayed && remotePeer.PeerSocketAddresses.Count == 0)
            {
                return false;
            }

            bool probablyDead = _offlineMap.ContainsKey(remotePeer.PeerId) ||
                                _shutdownMap.ContainsKey(remotePeer.PeerId) ||
                                _exceptionMap.ContainsKey(remotePeer.PeerId);
            if (thirdHand && probablyDead)
            {
                Logger.Debug("Don't add {0}.", remotePeer.PeerId);
                return false;
            }

            int classMember = ClassMember(remotePeer.PeerId);

            // the peer might have a new port
            var oldPeerStatistic = UpdateExistingVerifiedPeerAddress(PeerMapVerified[classMember], remotePeer,
                firstHand);
            if (oldPeerStatistic != null)
            {
                // we update the peer, so we can exit here and report that we have updated it
                NotifyUpdate(remotePeer, oldPeerStatistic);
                return true;
            }
            else
            {
                if (firstHand || (secondHand && !_peerVerification))
                {
                    var map = PeerMapVerified[classMember];
                    bool inserted = false;
                    lock (map)
                    {
                        // check again, now we are synchronized
                        if (map.ContainsKey(remotePeer.PeerId))
                        {
                            return PeerFound(remotePeer, referrer, peerConnection);
                        }
                        if (map.Count < BagSizeVerified)
                        {
                            var peerStatistic = new PeerStatistic(remotePeer);
                            peerStatistic.SuccessfullyChecked();
                            map.Add(remotePeer.PeerId, peerStatistic);
                            inserted = true;
                        }
                    }

                    if (inserted)
                    {
                        // if we inserted into the verified map, remove it from the non-verified map
                        var mapOverflow = PeerMapOverflow[classMember];
                        lock (mapOverflow)
                        {
                            mapOverflow.Remove(remotePeer.PeerId);
                        }
                        NotifyInsert(remotePeer, true);
                        return true;
                    }
                }
            }

            // if we are here, we did not have this peer, but our verified map was full
            // check if we have it stored in the non-verified map
            var mapOverflow2 = PeerMapOverflow[classMember];
            lock (mapOverflow2)
            {
                PeerStatistic peerStatistic;
                if (!mapOverflow2.TryGetValue(remotePeer.PeerId, out peerStatistic))
                {
                    peerStatistic = new PeerStatistic(remotePeer);
                }
                if (firstHand)
                {
                    peerStatistic.SuccessfullyChecked();
                }
                mapOverflow2.Add(remotePeer.PeerId, peerStatistic);
            }
            NotifyInsert(remotePeer, false);
            return true;
        }

        /// <summary>
        /// Removes a peer from the list. In order to not reappear, the node is put in a cache list
        /// for a certain time to keep the node removed. This method is thread-safe.
        /// </summary>
        /// <param name="remotePeer">The node to be removed.</param>
        /// <param name="peerException"></param>
        /// <returns>True, if the neighbor was removed and added to a cache list.
        /// False, if it has not been removed or is already in the temporarily removed list.</returns>
        public bool PeerFailed(PeerAddress remotePeer, PeerException peerException)
        {
            Logger.Debug("Peer {0} is offline with reason: {1}", remotePeer, peerException);

            // TB: ignore zero peer ID for the moment, but we should filter for the IP address
            if (remotePeer.PeerId.IsZero || Self.Equals(remotePeer.PeerId))
            {
                return false;
            }
            int classMember = ClassMember(remotePeer.PeerId);
            var reason = peerException.AbortCause;
            if (reason != PeerException.AbortCauseEnum.Timeout)
            {
                if (reason == PeerException.AbortCauseEnum.ProbablyOffline)
                {
                    _offlineMap.Put(remotePeer.PeerId, remotePeer);
                }
                else if (reason == PeerException.AbortCauseEnum.Shutdown)
                {
                    _shutdownMap.Put(remotePeer.PeerId, remotePeer);
                }
                else
                {
                    // reason is exception
                    _exceptionMap.Put(remotePeer.PeerId, remotePeer);
                }
                var tmp = PeerMapOverflow[classMember];
                if (tmp != null)
                {
                    lock (tmp)
                    {
                        tmp.Remove(remotePeer.PeerId);
                    }
                }
                tmp = PeerMapVerified[classMember];
                if (tmp != null)
                {
                    bool removed = false;
                    PeerStatistic peerStatistic;
                    lock (tmp)
                    {
                        peerStatistic = tmp.Remove2(remotePeer.PeerId);
                        if (peerStatistic != null)
                        {
                            removed = true;
                        }
                    }
                    if (removed)
                    {
                        NotifyRemove(remotePeer, peerStatistic);
                        return true;
                    }
                }
                return false;
            }
            // not forced
            if (UpdatePeerStatistic(remotePeer, PeerMapVerified[classMember], _offlineCount))
            {
                return PeerFailed(remotePeer,
                    new PeerException(PeerException.AbortCauseEnum.ProbablyOffline, "Peer failed in verified map."));
            }
            if (UpdatePeerStatistic(remotePeer, PeerMapOverflow[classMember], _offlineCount))
            {
                return PeerFailed(remotePeer,
                    new PeerException(PeerException.AbortCauseEnum.ProbablyOffline, "Peer failed in overflow map."));
            }
            return false;
        }

        /// <summary>
        /// Checks if a peer address is in the verified map.
        /// </summary>
        /// <param name="peerAddress">The peer address to check.</param>
        /// <returns>True, if the peer address is in the verified map.</returns>
        public bool Contains(PeerAddress peerAddress)
        {
            int classMember = ClassMember(peerAddress.PeerId);
            if (classMember == -1)
            {
                // -1 means we searched for ourself and we never are our neighbor
                return false;
            }
            var tmp = PeerMapVerified[classMember];
            lock (tmp)
            {
                return tmp.ContainsKey(peerAddress.PeerId);
            }
        }

        /// <summary>
        /// Checks if a peer address is in the overflow / non-verified map.
        /// </summary>
        /// <param name="peerAddress">The peer address to check.</param>
        /// <returns>True, if the peer address is in the overflow / non-verified map.</returns>
        public bool ContainsOverflow(PeerAddress peerAddress)
        {
            int classMember = ClassMember(peerAddress.PeerId);
            if (classMember == -1)
            {
                // -1 means we searched for ourself and we never are our neighbor
                return false;
            }
            var tmp = PeerMapOverflow[classMember];
            lock (tmp)
            {
                return tmp.ContainsKey(peerAddress.PeerId);
            }
        }

        /// <summary>
        /// Returns close peers to the peer itself.
        /// </summary>
        /// <param name="atLeast">The number we want to find at least.</param>
        /// <returns>A sorted set with close peers first in this set.</returns>
        public SortedSet<PeerAddress> ClosePeers(int atLeast)
        {
            return ClosePeers(Self, atLeast);
        }

        /// <summary>
        /// Returns close peers from the set to a given key. This method is thread-safe.
        /// You can use the returned set as it is a copy of the actual peer map and changes
        /// in the return set do not affect the peer map.
        /// </summary>
        /// <param name="id">The key that should be close to the keys in the map.</param>
        /// <param name="atLeast">The number we want to find at least.</param>
        /// <returns>A sorted set with close peers first in this set.</returns>
        public SortedSet<PeerAddress> ClosePeers(Number160 id, int atLeast)
        {
            return ClosePeers(Self, id, atLeast, PeerMapVerified);
        }

        public static SortedSet<PeerAddress> ClosePeers(Number160 self, Number160 other, int atLeast,
            IList<IDictionary<Number160, PeerStatistic>> peerMap)
        {
            var set = new SortedSet<PeerAddress>(CreateComparer(other));
            int classMember = ClassMember(self, other);
            // special treatment, as we can start iterating from 0
            if (classMember == -1)
            {
                for (int j = 0; j < Number160.Bits; j++)
                {
                    var tmp = peerMap[j];
                    if (FillSet(atLeast, set, tmp))
                    {
                        return set;
                    }
                }
                return set;
            }

            var tmp2 = peerMap[classMember];
            if (FillSet(atLeast, set, tmp2))
            {
                return set;
            }

            // in this case we have to go over all the bags that are smaller
            bool last = false;
            for (int i = 0; i < classMember; i++)
            {
                tmp2 = peerMap[i];
                last = FillSet(atLeast, set, tmp2);
            }
            if (last)
            {
                return set;
            }
            // in this case we have to go over all the bags that are larger
            for (int i = 0; i < Number160.Bits; i++)
            {
                tmp2 = peerMap[i];
                FillSet(atLeast, set, tmp2);
            }
            return set;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("I'm node ").Append(Self).Append("\n");
            for (int i = 0; i < Number160.Bits; i++)
            {
                var tmp = PeerMapVerified[i];
                lock (tmp)
                {
                    if (tmp.Count > 0)
                    {
                        sb.Append("class: ").Append(i).Append(" -> ");
                        foreach (var node in tmp.Values)
                        {
                            sb.Append("node: ").Append(node.PeerAddress).Append(",  ");
                        }
                        sb.Append("\n");
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates an XOR comparer based on this peer ID.
        /// </summary>
        /// <returns>The XOR comparer.</returns>
        public IComparer<PeerAddress> CreateComparer()
        {
            return CreateComparer(Self);
        }

        /// <summary>
        /// Creates the Kademlia distance comparer.
        /// </summary>
        /// <param name="id">The ID of this peer.</param>
        /// <returns>The XOR comparer.</returns>
        public static IComparer<PeerAddress> CreateComparer(Number160 id)
        {
            return new KademliaComparer(id);
        }

        /// <summary>
        /// Returns all addresses from the neighbor list. The collection is a copy
        /// and it is partially sorted.
        /// </summary>
        public IList<PeerAddress> All
        {
            get
            {
                var all = new List<PeerAddress>();
                foreach (var map in PeerMapVerified)
                {
                    lock (map)
                    {
                        foreach (var peerStatistic in map.Values)
                        {
                            all.Add(peerStatistic.PeerAddress);
                        }
                    }
                }
                return all;
            }
        }

        /// <summary>
        /// Returns all addresses from the overflow / non-verified list. The collection is a copy
        /// and it is partially sorted.
        /// </summary>
        public IList<PeerAddress> AllOverflow
        {
            get
            {
                var allOverflow = new List<PeerAddress>();
                foreach (var map in PeerMapOverflow)
                {
                    lock (map)
                    {
                        foreach (var peerStatistic in map.Values)
                        {
                            allOverflow.Add(peerStatistic.PeerAddress);
                        }
                    }
                }
                return allOverflow;
            }
        }

        /// <summary>
        /// Checks of a peer is in the offline map.
        /// </summary>
        /// <param name="peerAddress">The peer address to look for.</param>
        /// <returns>True, if the peer is in the offline map, meaning that we consider this
        /// peer offline.</returns>
        public bool IsPeerRemovedTemporarly(PeerAddress peerAddress)
        {
            return _offlineMap.ContainsKey(peerAddress.PeerId)
                   || _shutdownMap.ContainsKey(peerAddress.PeerId)
                   || _exceptionMap.ContainsKey(peerAddress.PeerId);
        }

        /// <summary>
        /// Finds the next peer that should have a maintenance check. Returns null if no maintenance 
        /// is needed at the moment. It will return the most important peers first. Importance is as 
        /// follows: The most important peers are the close ones in the verified peer map. If a certain
        /// threshold in a bag is not reached, the unverified becomes important too.
        /// </summary>
        /// <param name="notInterestedAddresses"></param>
        /// <returns>The next most important peer to check if it is still alive.</returns>
        public PeerStatistic NextForMaintenance(ICollection<PeerAddress> notInterestedAddresses)
        {
            return _maintenance.NextForMaintenance(notInterestedAddresses);
        }

        /// <summary>
        /// Returns the number of the class that this ID belongs to.
        /// </summary>
        /// <param name="remoteId">The ID to test.</param>
        /// <returns>The number of bits used in the difference.</returns>
        private int ClassMember(Number160 remoteId)
        {
            return ClassMember(Self, remoteId);
        }

        /// <summary>
        /// Returns the difference in terms of bit counts of two IDs, minus 1.
        /// So two IDs with one bit difference are in the class 0.
        /// </summary>
        /// <param name="id1">The first ID.</param>
        /// <param name="id2">The second ID.</param>
        /// <returns>The bit difference and -1 if they are equal.</returns>
        internal static int ClassMember(Number160 id1, Number160 id2)
        {
            return Distance(id1, id2).BitLength - 1;
        }

        /// <summary>
        /// The distance metric is the XOR metric.
        /// </summary>
        /// <param name="id1">The first ID.</param>
        /// <param name="id2">The second ID.</param>
        /// <returns>The XOR distance.</returns>
        internal static Number160 Distance(Number160 id1, Number160 id2)
        {
            return id1.Xor(id2);
        }

        /// <summary>
        /// Returns -1 if the first remote node is closer to the key.
        /// If the second node is closer, then 1 is returned.
        /// If both are equal, 0 is returned.
        /// </summary>
        /// <param name="id">The ID as a distance reference.</param>
        /// <param name="rn">The peer to test if closer to the ID.</param>
        /// <param name="rn2">The other peer to test if closer to the ID.</param>
        /// <returns>-1 if the first peer is closer, 1 otherwise. 0 if both are equal.</returns>
        public static int IsKadCloser(Number160 id, PeerAddress rn, PeerAddress rn2)
        {
            return Distance(id, rn.PeerId).CompareTo(Distance(id, rn2.PeerId));
        }

        /// <summary>
        /// Updates the peer statistics and checks if the max failure has been reached.
        /// </summary>
        /// <param name="remotePeer">The remote peer.</param>
        /// <param name="tmp">The bag of where the peer is supposed to be.</param>
        /// <param name="maxFail">The number of max failure until a peer is considered offline.</param>
        /// <returns>True, if this peer is considered offline. False, otherwise.</returns>
        private static bool UpdatePeerStatistic(PeerAddress remotePeer, IDictionary<Number160, PeerStatistic> tmp,
            int maxFail)
        {
            if (tmp != null)
            {
                lock (tmp)
                {
                    PeerStatistic peerStatistic;
                    if (tmp.TryGetValue(remotePeer.PeerId, out peerStatistic))
                    {
                        if (peerStatistic.Failed() >= maxFail)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a peer already exists in this map and if it does, it will update the entry
        /// because the peer address (e.g., port) may have changed.
        /// </summary>
        /// <param name="tmp">The map where the peer is supposed to be.</param>
        /// <param name="peerAddress">The address of the peer that may have been changed.</param>
        /// <param name="firstHand">True, if this peer sent and received a message from the remote peer.</param>
        /// <returns>The old peer address if we have updated the peer. Null, otherwise.</returns>
        private static PeerStatistic UpdateExistingVerifiedPeerAddress(IDictionary<Number160, PeerStatistic> tmp,
            PeerAddress peerAddress, bool firstHand)
        {
            lock (tmp)
            {
                PeerStatistic old;
                if (tmp.TryGetValue(peerAddress.PeerId, out old))
                {
                    // TODO from Java: this should only be from firsthand
                    old.SetPeerAddress(peerAddress);
                    if (firstHand)
                    {
                        old.SuccessfullyChecked();
                    }
                    return old;
                }
            }
            return null;
        }

        /// <summary>
        /// Fills the set with peer addresses. Fills it until a limit is reached.
        /// However, this is a soft limit, as the bag may contain close peers in
        /// a random manner.
        /// </summary>
        /// <param name="atLeast">The number of addresses we want at least.
        /// It does not matter if it is more.</param>
        /// <param name="set">The set where to store the results.</param>
        /// <param name="tmp">The bag where to take the addresses from.</param>
        /// <returns>True, if the desired size has been reached.</returns>
        private static bool FillSet(int atLeast, SortedSet<PeerAddress> set, IDictionary<Number160, PeerStatistic> tmp)
        {
            lock (tmp)
            {
                foreach (var peerStatistic in tmp.Values)
                {
                    set.Add(peerStatistic.PeerAddress);
                }
            }
            return set.Count >= atLeast;
        }

        /// <summary>
        /// The Kademlia distance comparer.
        /// </summary>
        private class KademliaComparer : IComparer<PeerAddress>
        {
            private readonly Number160 _id;

            /// <summary>
            /// Creates a Kademlia distance comparer.
            /// </summary>
            /// <param name="id">The ID of this peer.</param>
            public KademliaComparer(Number160 id)
            {
                _id = id;
            }

            public int Compare(PeerAddress remotePeer, PeerAddress remotePeer2)
            {
                return IsKadCloser(_id, remotePeer, remotePeer2);
            }
        }
    }
}
