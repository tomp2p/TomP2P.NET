namespace TomP2P.Core.Peers
{
    /// <summary>
    /// Filters potential and direct hits from the result set.
    /// </summary>
    public interface IPostRoutingFilter
    {
        /// <summary>
        /// Rejects or accepts a potential hit.
        /// </summary>
        /// <param name="peerAddress">The peer address under question.</param>
        /// <returns>True, if rejected. False, if accepted.</returns>
        bool RejectPotentialHit(PeerAddress peerAddress);

        /// <summary>
        /// Rejects or accepts a direct hit.
        /// </summary>
        /// <param name="peerAddress">The peer address under question.</param>
        /// <returns>True, if rejected. False, if accepted.</returns>
        bool RejectDirectHit(PeerAddress peerAddress);
    }
}
