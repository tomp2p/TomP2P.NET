using TomP2P.Message;
using TomP2P.Peers;

namespace TomP2P.Rpc
{
    /// <summary>
    /// The interface for receiving raw data and sending raw data back.
    /// Raw means that we use a Netty buffer.
    /// </summary>
    public interface IRawDataReply
    {
        /// <summary>
        /// Replies to a direct message from a peer.
        /// </summary>
        /// <param name="sender">The sender from which the request came.</param>
        /// <param name="requestBuffer">The incoming buffer.</param>
        /// <param name="complete">Indication if the request buffer is complete.</param>
        /// <returns>A buffer with the result. If null is returned, then the message will contain
        /// NotFound, if the same buffer as requestBuffer is sent back, the message will contain Ok.
        /// Otherwise, the payload will be set.</returns>
        Buffer Reply(PeerAddress sender, Buffer requestBuffer, bool complete);
    }
}
