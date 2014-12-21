using TomP2P.Peers;

namespace TomP2P.P2P
{
    /// <summary>
    /// Use this interface to notify if a peer received a broadcast ping.
    /// </summary>
    public interface IPeerReceivedBroadcastPing
    {
        /// <summary>
        /// Call this method when we receive a broadcast ping.
        /// If multiple peers are on the same network, only one reply will be accepted.
        /// Thus, all peers that receive such a broadcast ping will call this method.
        /// </summary>
        /// <param name="sender">The sender that sent the broadcast ping.</param>
        void BroadcastPingReceived(PeerAddress sender);
    }
}
