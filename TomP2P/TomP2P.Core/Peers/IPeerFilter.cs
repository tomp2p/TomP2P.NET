using System.Collections.Generic;

namespace TomP2P.Core.Peers
{
    /// <summary>
    /// A filter that can prevent peers from being stored in the map.
    /// </summary>
    public interface IPeerFilter
    {
        /// <summary>
        /// Each peer that is added in the map runs through this filter.
        /// </summary>
        /// <param name="peerAddress">The peer address that is going to be added to the map.</param>
        /// <param name="all"></param>
        /// <param name="target"></param>
        /// <returns>True, if the peer address should not be added. False, otherwise.</returns>
        bool Reject(PeerAddress peerAddress, ICollection<PeerAddress> all, Number160 target);
    }
}
