using TomP2P.Futures;
using TomP2P.Peers;

namespace TomP2P.Connection
{
    /// <summary>
    /// All classes that are interested if a new peer was discovered or a peer 
    /// died (that means all classes that store peer addresses) should implement
    /// this interface and add itself as a listener.
    /// </summary>
    public interface IPeerStatusListener
    {
        /// <summary>
        /// Called if the peer does not send answer in time. 
        /// The peer may be busy, so there is a chance of seeing this peer again.
        /// </summary>
        /// <param name="remotePeer">The address of the peer that failed.</param>
        /// <param name="exception">The reason why the peer failed. This is important 
        /// to understand if we can re-enable the peer.</param>
        /// <returns>False, if nothing happened. True, if there was a change.</returns>
        bool PeerFailed(PeerAddress remotePeer, PeerException exception);

        /// <summary>
        /// Called if the peer is online. Provides the referrer who reported it. This 
        /// method may get called many times, for each successful request.
        /// </summary>
        /// <param name="remotePeer">The address of the peer that is online.</param>
        /// <param name="referrer">The peer that reported the availability of the peer address.</param>
        /// <param name="peerConnection"></param>
        /// <returns>False, if nothing happened. True, if there was a change.</returns>
        bool PeerFound(PeerAddress remotePeer, PeerAddress referrer, PeerConnection peerConnection);
    }
}
