
namespace TomP2P.Connection.Pipeline
{
    /// <summary>
    /// Equivalent to Java Netty's ChannelInboundHandlerInterface
    /// </summary>
    public interface IInboundHandler
    {
        // TODO ctx needed?
        /// <summary>
        /// Invoked when the current channel has read a message from the peer.
        /// </summary>
        /// <param name="message"></param>
        void MessageReceived(Message.Message message);
    }
}
