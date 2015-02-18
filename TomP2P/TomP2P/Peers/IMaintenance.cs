using System.Collections.Generic;
using TomP2P.Utils;

namespace TomP2P.Peers
{
    /// <summary>
    /// Maintenance is importatn in an iterative P2P network.
    /// Thus we need to identify the important peers and start polling them.
    /// </summary>
    public interface IMaintenance
    {
        /// <summary>
        /// Initializes the maintenance class. This may result in a new class.
        /// </summary>
        /// <param name="peerMapVerified">The map with the bags of verified peers.</param>
        /// <param name="peerMapNonVerified">The map with the bags of non-verified peers.</param>
        /// <param name="offlineMap">The map with the offline peers.</param>
        /// <param name="shutdownMap">The map with the peers that quit friendly.</param>
        /// <param name="exceptionMap">The map with the peers that caused an exception.</param>
        /// <returns>The same or a new maintenance class.</returns>
        IMaintenance Init(IList<IDictionary<Number160, PeerStatistic>> peerMapVerified,
            IList<IDictionary<Number160, PeerStatistic>> peerMapNonVerified,
            ConcurrentCacheMap<Number160, PeerAddress> offlineMap,
            ConcurrentCacheMap<Number160, PeerAddress> shutdownMap,
            ConcurrentCacheMap<Number160, PeerAddress> exceptionMap);

        /// <summary>
        /// Returns the next peer that needs maintenance or null if no maintenance is needed.
        /// </summary>
        /// <param name="notInterestedAddress"></param>
        /// <returns></returns>
        PeerStatistic NextForMaintenance(ICollection<PeerAddress> notInterestedAddress);
    }
}
