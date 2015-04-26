using System.Collections.Generic;

namespace TomP2P.Core.Peers
{
    /// <summary>
    /// A filter that can prevent peers from being stored in the map.
    /// Therefore, it will be ignored in the routing.
    /// </summary>
    public interface IPeerMapFilter
    {
        /// <summary>
        /// Each peer that is added in the map runs through this filter.
        /// </summary>
        /// <param name="peerAddress">The peer address that is going to be added to the map.</param>
        /// <param name="peerMap">The peer map where additional information can be retrieved.</param>
        /// <returns>True, if the peer address should not be added, false otherwise.</returns>
        bool RejectPeerMap(PeerAddress peerAddress, PeerMap peerMap);

        bool RejectPreRouting(PeerAddress peerAddress, IEnumerable<PeerAddress> all);
    }
}
