namespace TomP2P.Peers
{
    /// <summary>
    /// This interface can be added to the map to get notified of peer insertion or removal.
    /// This is useful for replication.
    /// </summary>
    public interface IPeerMapChangeListener
    {
        /// <summary>
        /// This method is called if a peer is added to the map.
        /// The peer is always added to the non-verified map first.s
        /// </summary>
        /// <param name="peerAddress">The address of the peer that has been added.</param>
        /// <param name="verified">True, if the peer was inserted in the verified map.</param>
        void PeerInserted(PeerAddress peerAddress, bool verified);

        /// <summary>
        /// This method is called if a  peer is removed from the map.
        /// </summary>
        /// <param name="peerAddress">The address of the peer that has been removed.</param>
        /// <param name="storedPeerAddress">Contains statistical information.</param>
        void PeerRemoved(PeerAddress peerAddress, PeerStatistic storedPeerAddress);

        /// <summary>
        /// This method is called if a peer is updated.
        /// </summary>
        /// <param name="peerAddress">The address of the peer that has been updated.</param>
        /// <param name="storedPeerAddress">Contains statistical information.</param>
        void PeerUpdated(PeerAddress peerAddress, PeerStatistic storedPeerAddress);
    }
}
