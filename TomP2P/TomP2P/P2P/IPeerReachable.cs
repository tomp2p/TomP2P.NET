using TomP2P.Peers;

namespace TomP2P.P2P
{
    /// <summary>
    /// Use this interface to notify if a peer is reachable from the outside.
    /// </summary>
    public interface IPeerReachable
    {
        /// <summary>
        /// Call this method when other peers can reach our peer from outside.
        /// </summary>
        /// <param name="peerAddress">How we can be reached from outside.</param>
        /// <param name="reporter">The reporter that told us we are reachable.</param>
        /// <param name="tcp">True, if we are reachable over TCP. False, if we are reachable over UDP.</param>
        void PeerWellConnected(PeerAddress peerAddress, PeerAddress reporter, bool tcp);
    }
}
