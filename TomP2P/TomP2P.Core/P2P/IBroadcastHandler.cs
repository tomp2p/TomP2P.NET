namespace TomP2P.Core.P2P
{
    /// <summary>
    /// The handler that is called when a broadcast message is received.
    /// One way to implement this would be to send it to random peers.
    /// </summary>
    public interface IBroadcastHandler
    {
        /// <summary>
        /// This method is called when a peer receives a broadcast message request.
        /// It is up to the peer to decide what to do with it.
        /// </summary>
        /// <param name="message"></param>
        void Receive(Message.Message message);
    }
}
