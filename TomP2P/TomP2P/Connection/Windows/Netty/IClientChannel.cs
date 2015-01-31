using System.Threading.Tasks;

namespace TomP2P.Connection.Windows.Netty
{
    /// <summary>
    /// Interface for client-side channels that send and retrieve messages.
    /// </summary>
    public interface IClientChannel : IChannel
    {
        Task SendMessageAsync(Message.Message message);

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
