using TomP2P.Peers;

namespace TomP2P.Rpc
{
    /// <summary>
    /// Similar to <see cref="IRawDataReply"/>, but we convert the raw byte buffer to an object.
    /// </summary>
    public interface IObjectDataReply
    {
        /// <summary>
        /// Replies to a direct message from a peer. This reply is based on objects.
        /// </summary>
        /// <param name="sender">The sender of this message.</param>
        /// <param name="request">The request that the sender sent.</param>
        /// <returns>A new object that is the reply.</returns>
        object Reply(PeerAddress sender, object request);
    }
}
