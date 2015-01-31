using System.Net.Sockets;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows.Netty
{
    public delegate void ClosedEventHandler(IChannel channel);
    
    /// <summary>
    /// Interface to expose Java Netty's Channel API that is needed for this project.
    /// </summary>
    public interface IChannel
    {
        event ClosedEventHandler Closed;

        void Close();

        Task SendMessageAsync(Message.Message message);

        /// <summary>
        /// The underlying socket that is used.
        /// </summary>
        Socket Socket { get; }

        Pipeline Pipeline { get; }

        bool IsUdp { get; }

        bool IsTcp { get; }
    }
}
