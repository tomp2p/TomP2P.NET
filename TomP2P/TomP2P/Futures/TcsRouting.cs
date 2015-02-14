using System.Collections.Generic;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.Futures
{
    /// <summary>
    /// The routing task keeps track of the routing process.
    /// The routing will always succeed if we do DHT operations or bootstrap to ourself.
    /// It will fail if we bootstrap to another peer, but could not contact any peer than ourself.
    /// </summary>
    public class TcsRouting : BaseTcsImpl
    {
        private SortedSet<PeerAddress> _potentialHits;
        private SortedDictionary<PeerAddress, DigestInfo> _directHits;
        private SortedSet<PeerAddress> _routingPath;

        public void SetNeighbors(SortedDictionary<PeerAddress, DigestInfo> directHits,
            SortedSet<PeerAddress> potentialHits, SortedSet<PeerAddress> routingPath, bool isBootstrap,
            bool isRoutingToOther)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return;
                }
                _potentialHits = potentialHits;
                _directHits = directHits;
                _routingPath = routingPath;
                if (isBootstrap && isRoutingToOther)
                {
                    // We need to fail if we only find ourself. This means that we did 
                    // not connect to any peer and we did not want to connect to ourself.
                    // TODO type needed?
                }
                else
                {
                    // For DHT or bootstrapping to ourself, we set to success since we may
                    // want to store data on our peer rather than failing completely if
                    // we don't find other peers.
                    // TODO type needed?
                }
            }
            NotifyListeners();
        }

        /// <summary>
        /// The potential hits set contains those peers that are in the direct hit
        /// and that did report to *not* have the key (Number160) we were looking
        /// for. We already checked for the content during routing, since we sent the
        /// information what we are looking for anyway. So a reply if the content
        /// exists or not is not very expensive. However, a peer may lie about this.
        /// </summary>
        public SortedSet<PeerAddress> PotentialHits
        {
            get
            {
                lock (Lock)
                {
                    return _potentialHits;
                }
            }
        }

        /// <summary>
        /// The direct hits set contains those peers that reported to have the key
        /// (Number160) we were looking for. We already checked for the content during
        /// routing, since we sent the information what we are looking for anyway. So
        /// a reply if the content exists or not is not very expensive. However, a peer
        /// may lie about this.
        /// </summary>
        public SortedSet<PeerAddress> DirectHits
        {
            get
            {
                lock (Lock)
                {
                    if (_directHits == null)
                    {
                        return null;
                    }
                    var tmp = _directHits.Keys;
                    var tmp2 = new SortedSet<PeerAddress>(tmp, _directHits.Comparer);
                    return tmp2;
                }
            }
        }

        /// <summary>
        /// The direct hits map contains those peers that reported to have the key
        /// (Number160) we were looking for including its digest (size of the result
        /// set and its XOR-ed hashes). We already checked for the content during
        /// routing, since we sent the information what we are looking for anyway. So
        /// a reply if the content exists or not is not very expensive. However, a 
        /// peer may lie about this.
        /// </summary>
        public SortedDictionary<PeerAddress, DigestInfo> DirectHitsDigest
        {
            get
            {
                lock (Lock)
                {
                    return _directHits;
                }
            }
        }

        /// <summary>
        /// Returns the peers that have been asked to provide neighbor information.
        /// The order is sorted by peers that were close to the target.
        /// </summary>
        /// <returns>A set of peers that took part in the routing process.</returns>
        public SortedSet<PeerAddress> RoutingPath()
        {
            lock (Lock)
            {
                return _routingPath;
            }
        }
    }
}
