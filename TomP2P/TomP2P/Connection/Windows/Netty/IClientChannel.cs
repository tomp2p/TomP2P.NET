using System.Threading.Tasks;

namespace TomP2P.Connection.Windows.Netty
{
    /// <summary>
    /// Interface for client-side channels that send and retrieve messages.
    /// </summary>
    public interface IClientChannel : IChannel
    {
        /// <summary>
        /// Executes the client-side outbound pipeline and sends message over the wire.
        /// </summary>
        /// <param name="message">The request message to be sent.</param>
        /// <returns></returns>
        Task SendMessageAsync(Message.Message message);

        /// <summary>
        /// Receives bytes from the remote host and executes the client-side inbound pipeline.
        /// </summary>
        /// <returns></returns>
        Task ReceiveMessageAsync();
    }

    /// <summary>
    /// Marker interface for client-side TCP channels.
    /// </summary>
    public interface ITcpClientChannel : ITcpChannel, IClientChannel
    {
    }

    /// <summary>
    /// Marker interface for client-side UDP channels.
    /// </summary>
    public interface IUdpClientChannel : IUdpChannel, IClientChannel
    {
    }
}
